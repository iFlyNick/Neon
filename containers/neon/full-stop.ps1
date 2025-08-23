$envFile = ".\.env"

docker compose --env-file $envFile -f .\scalable\docker-compose.yml down

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to stop the scalable containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "Scalable containers stopped successfully."

docker compose --env-file $envFile -f .\core\docker-compose.yml down

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to stop the core containers. Please check the logs for more details."
    exit $LASTEXITCODE
}

Write-Host "Core containers stopped successfully."