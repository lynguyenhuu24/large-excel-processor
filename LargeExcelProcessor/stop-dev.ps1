Write-Host "[*] Stopping dotnet processes..."
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "[*] Stopping func processes..."
Get-Process -Name func -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "[*] Stopping ng/node processes..."
Get-Process -Name node -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match 'angular|ng' -or $_.CommandLine -match '\\ng ' } | Stop-Process -Force

Write-Host "[*] Stopping Docker containers..."
docker compose -f "$PSScriptRoot/docker-compose.yml" down

Write-Host "[+] All services stopped."
