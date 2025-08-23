$envFile = "..\.env"

docker compose --env-file $envFile -f .\docker-compose.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start the core containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "Core containers started successfully."