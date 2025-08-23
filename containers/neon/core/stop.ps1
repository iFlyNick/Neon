$envFile = "..\.env"

docker compose --env-file $envFile -f .\docker-compose.yml down

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to stop the core containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "Core containers stopped successfully."