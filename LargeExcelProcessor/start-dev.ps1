$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSCommandPath

Write-Host "[*] Starting Docker containers (PostgreSQL + Azurite)..."
docker compose -f "$root/docker-compose.yml" up -d
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Host "[*] Waiting for PostgreSQL health check..."
do {
  $healthy = docker inspect --format='{{.State.Health.Status}}' large-excel-pg 2>$null
  Start-Sleep -Seconds 2
} while ($healthy -ne 'healthy')

Write-Host "[*] Launching .NET API (localhost:5000)..."
$apiDir = "$root/src/LargeExcelProcessor.Api"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$apiDir'; dotnet run --launch-profile http"

Write-Host "[*] Launching Azure Functions (blob trigger)..."
$funcDir = "$root/src/LargeExcelProcessor.Functions"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$funcDir'; func start"

Write-Host "[*] Launching Angular dev server (localhost:4200)..."
$ngDir = "$root/frontend/large-excel-processor-ui"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$ngDir'; ng serve"

Write-Host "[+] Dev environment started. Close windows or run stop-dev.ps1 to shut down."
