$envFile = ".\.env"

docker compose --env-file $envFile -f .\core\docker-compose.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start the core containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "Core containers started successfully."

docker compose --env-file $envFile -f .\scalable\docker-compose.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start the scalable containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "scalable containers started successfully."