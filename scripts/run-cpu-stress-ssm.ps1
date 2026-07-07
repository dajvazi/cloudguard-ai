# Run CPU stress on EC2 via AWS SSM (no SSH required)
param(
    [string]$InstanceId = "i-096014610bc8fdd16",
    [int]$DurationSeconds = 1800,
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"
$envFile = Join-Path $PSScriptRoot "..\backend\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            Set-Item -Path "env:$($matches[1].Trim())" -Value $matches[2].Trim()
        }
    }
}

$aws = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
if (-not (Test-Path $aws)) { $aws = "aws" }

$state = & $aws ec2 describe-instances --instance-ids $InstanceId --region $Region `
    --query "Reservations[0].Instances[0].State.Name" --output text
Write-Host "EC2 state: $state"

if ($state -eq "stopped") {
    Write-Host "Starting instance..."
    & $aws ec2 start-instances --instance-ids $InstanceId --region $Region | Out-Null
    & $aws ec2 wait instance-running --instance-ids $InstanceId --region $Region
    Write-Host "Instance running. Waiting for SSM..."
    Start-Sleep -Seconds 45
}

$paramsJson = @'
{
  "commands": [
    "echo === CloudGuard CPU stress test ===",
    "CORES=$(nproc || echo 2)",
    "if command -v stress >/dev/null 2>&1; then nohup stress --cpu $CORES --timeout DURATION_PLACEHOLDER >/tmp/cloudguard_stress.log 2>&1 & else for i in $(seq 1 $CORES); do nohup timeout DURATION_PLACEHOLDER bash -c 'while true; do :; done' >/dev/null 2>&1 & done; fi",
    "echo CPU load started for DURATION_PLACEHOLDER seconds on $CORES cores",
    "uptime"
  ]
}
'@ -replace 'DURATION_PLACEHOLDER', "$DurationSeconds"

$paramsFile = Join-Path $env:TEMP "ssm-cpu-stress.json"
[System.IO.File]::WriteAllText($paramsFile, $paramsJson, [System.Text.UTF8Encoding]::new($false))
$paramUri = "file://" + ($paramsFile -replace '\\', '/')

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

Start-Sleep -Seconds 10
$result = & $aws ssm get-command-invocation `
    --command-id $commandId `
    --instance-id $InstanceId `
    --region $Region `
    --output json | ConvertFrom-Json

Write-Host "Status: $($result.Status)"
Write-Host "Output:"
Write-Host $result.StandardOutputContent
if ($result.StandardErrorContent) {
    Write-Host "Stderr:"
    Write-Host $result.StandardErrorContent
}

Write-Host ""
Write-Host "Done. Wait 2-5 min, then CloudGuard UI -> Import Cloud -> Last 1 hour -> Import"
