# Runs load-ec2-metrics.sh on the EC2 instance via AWS SSM (no SSH / no .pem needed).
param(
    [string]$InstanceId = "i-096014610bc8fdd16",
    [int]$CpuCores = 2,
    [int]$DurationSeconds = 1800,
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

# 1. Load AWS credentials from backend/.env
$envFile = Join-Path $PSScriptRoot "..\backend\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*(AWS[^=]+)=(.*)$') {
            Set-Item -Path "env:$($matches[1].Trim())" -Value $matches[2].Trim()
        }
    }
}

$aws = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
if (-not (Test-Path $aws)) { $aws = "aws" }

# 2. Ensure the instance is running
$state = & $aws ec2 describe-instances --instance-ids $InstanceId --region $Region `
    --query "Reservations[0].Instances[0].State.Name" --output text
Write-Host "EC2 state: $state"
if ($state -eq "stopped") {
    Write-Host "Starting instance..."
    & $aws ec2 start-instances --instance-ids $InstanceId --region $Region | Out-Null
    & $aws ec2 wait instance-running --instance-ids $InstanceId --region $Region
    Write-Host "Instance running. Waiting 45s for SSM agent..."
    Start-Sleep -Seconds 45
}

# 3. Base64-encode load-ec2-metrics.sh so no shell escaping issues occur
$loadScriptPath = Join-Path $PSScriptRoot "load-ec2-metrics.sh"
$raw = [System.IO.File]::ReadAllText($loadScriptPath)
$raw = $raw -replace "`r`n", "`n"          # normalize line endings to LF
$b64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($raw))

# 4. Build SSM parameter JSON (decode -> chmod -> run in background)
$paramsJson = @"
{
  "commands": [
    "echo '$b64' | base64 -d > /tmp/load-ec2-metrics.sh",
    "chmod +x /tmp/load-ec2-metrics.sh",
    "nohup /tmp/load-ec2-metrics.sh $CpuCores $DurationSeconds > /tmp/load.out 2>&1 &",
    "sleep 3",
    "echo '--- load started ---'",
    "uptime",
    "cat /tmp/load.out 2>/dev/null | head -20"
  ]
}
"@

$paramsFile = Join-Path $env:TEMP "ssm-load-params.json"
[System.IO.File]::WriteAllText($paramsFile, $paramsJson, [System.Text.UTF8Encoding]::new($false))
$paramUri = "file://" + ($paramsFile -replace '\\', '/')

# 5. Send the command
$send = & $aws ssm send-command `
    --instance-ids $InstanceId `
    --document-name "AWS-RunShellScript" `
    --parameters $paramUri `
    --region $Region `
    --output json | ConvertFrom-Json

Remove-Item $paramsFile -ErrorAction SilentlyContinue

$commandId = $send.Command.CommandId
Write-Host "SSM CommandId: $commandId"
Write-Host "Waiting for result..."

Start-Sleep -Seconds 12
$result = & $aws ssm get-command-invocation `
    --command-id $commandId `
    --instance-id $InstanceId `
    --region $Region `
    --output json | ConvertFrom-Json

Write-Host "Status: $($result.Status)"
Write-Host "--- Output ---"
Write-Host $result.StandardOutputContent
if ($result.StandardErrorContent) {
    Write-Host "--- Stderr ---"
    Write-Host $result.StandardErrorContent
}

Write-Host ""
Write-Host "CPU load running for $DurationSeconds s on $CpuCores cores."
Write-Host "Wait 2-5 min, then in CloudGuard UI: Import Cloud -> Last 1 hour -> Import"
