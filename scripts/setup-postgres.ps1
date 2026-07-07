#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

$pgData = "C:\Program Files\PostgreSQL\17\data"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$pgHba = Join-Path $pgData "pg_hba.conf"
$password = "1908"
$port = "1234"

Write-Host "==> Reloading PostgreSQL config access..."
$hba = Get-Content $pgHba -Raw
$originalHba = $hba
$hba = $hba -replace 'host\s+all\s+all\s+127\.0\.0\.1/32\s+scram-sha-256', 'host    all             all             127.0.0.1/32            trust'
if ($hba -eq $originalHba) {
    Write-Host "pg_hba.conf already allows trust or pattern not found; continuing..."
} else {
    Set-Content -Path $pgHba -Value $hba -Encoding UTF8
    & "$pgBin\pg_ctl.exe" reload -D $pgData
    Start-Sleep -Seconds 2
}

Write-Host "==> Setting postgres password and creating database..."
$env:PGPASSWORD = ""
$sql = @"
ALTER USER postgres WITH PASSWORD '$password';
SELECT 'CREATE DATABASE cloudguard OWNER postgres'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'cloudguard')\gexec
"@

& "$pgBin\psql.exe" -U postgres -h 127.0.0.1 -p $port -d postgres -v ON_ERROR_STOP=1 -c "ALTER USER postgres WITH PASSWORD '$password';"
& "$pgBin\psql.exe" -U postgres -h 127.0.0.1 -p $port -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'cloudguard'" | Out-Null
$exists = & "$pgBin\psql.exe" -U postgres -h 127.0.0.1 -p $port -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'cloudguard'"
if (-not $exists) {
    & "$pgBin\psql.exe" -U postgres -h 127.0.0.1 -p $port -d postgres -c "CREATE DATABASE cloudguard WITH OWNER = postgres ENCODING = 'UTF8';"
    Write-Host "Database 'cloudguard' created."
} else {
    Write-Host "Database 'cloudguard' already exists."
}

Write-Host "==> Restoring scram authentication..."
if ($hba -ne $originalHba) {
    Set-Content -Path $pgHba -Value $originalHba -Encoding UTF8
    & "$pgBin\pg_ctl.exe" reload -D $pgData
}

$env:PGPASSWORD = $password
& "$pgBin\psql.exe" -U postgres -h 127.0.0.1 -p $port -d cloudguard -c "\dt"
Write-Host "==> Done. PostgreSQL ready on port $port"
