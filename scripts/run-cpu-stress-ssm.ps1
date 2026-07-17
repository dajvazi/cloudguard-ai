# Run CPU stress on EC2 via AWS SSM (no SSH required).
# Enables 1-minute CloudWatch monitoring and waits until CPU is visible.
param(
    [string]$InstanceId = "i-096014610bc8fdd16",
    [int]$DurationSeconds = 1800,
    [string]$Region = "us-east-1",
    [int]$ReadyTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
$env:PYTHONIOENCODING = "utf-8"
$env:PYTHONUTF8 = "1"

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

# 1-minute CloudWatch datapoints (instead of basic 5-minute)
Write-Host "Enabling detailed monitoring (1-min metrics)..."
& $aws ec2 monitor-instances --instance-ids $InstanceId --region $Region | Out-Null

$paramsJson = @'
{
  "commands": [
    "echo === CloudGuard CPU stress test ===",
    "command -v stress >/dev/null 2>&1 || (sudo apt-get update -qq && sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq stress)",
    "CORES=$(nproc || echo 2)",
    "if command -v stress >/dev/null 2>&1; then nohup stress --cpu $CORES --timeout DURATION_PLACEHOLDER >/tmp/cloudguard_stress.log 2>&1 & else for i in $(seq 1 $CORES); do nohup timeout DURATION_PLACEHOLDER bash -c 'while true; do :; done' >/dev/null 2>&1 & done; fi",
    "sleep 2",
    "echo CPU load started for DURATION_PLACEHOLDER seconds on $CORES cores",
    "pgrep -a stress || true",
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
Write-Host "Waiting for SSM..."

$status = "Pending"
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    $result = & $aws ssm get-command-invocation `
        --command-id $commandId `
        --instance-id $InstanceId `
        --region $Region `
        --output json | ConvertFrom-Json
    $status = $result.Status
    if ($status -eq "Success" -or $status -eq "Failed" -or $status -eq "Cancelled" -or $status -eq "TimedOut") {
        break
    }
}

Write-Host "Status: $status"
Write-Host "Output:"
Write-Host $result.StandardOutputContent
if ($result.StandardErrorContent) {
    Write-Host "Stderr:"
    Write-Host $result.StandardErrorContent
}

Write-Host ""
Write-Host "Polling CloudWatch CPU (max ${ReadyTimeoutSeconds}s)..."
$ready = $false
$deadline = [DateTime]::UtcNow.AddSeconds($ReadyTimeoutSeconds)
while ([DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Seconds 15
    $end = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
    $start = [DateTime]::UtcNow.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")
    $cpu = & $aws cloudwatch get-metric-statistics `
        --namespace AWS/EC2 `
        --metric-name CPUUtilization `
        --dimensions "Name=InstanceId,Value=$InstanceId" `
        --start-time $start `
        --end-time $end `
        --period 60 `
        --statistics Average `
        --region $Region `
        --query "Datapoints | max_by(@, &Timestamp).Average" `
        --output text 2>$null

    if ($cpu -and $cpu -ne "None" -and [double]$cpu -ge 40) {
        Write-Host ("CPU ready: {0:N1}%" -f [double]$cpu)
        $ready = $true
        break
    }

    $shown = if ($cpu -and $cpu -ne "None") { ("{0:N1}%" -f [double]$cpu) } else { "no datapoint yet" }
    Write-Host "  still waiting... ($shown)"
}

Write-Host ""
if ($ready) {
    Write-Host "Ready now. CloudGuard UI -> Import Cloud -> Now (last 5 minutes) -> Import"
} else {
    Write-Host "Stress is running, but CloudWatch has not published high CPU yet."
    Write-Host "Try Import Cloud -> Now in about 1 minute."
}
