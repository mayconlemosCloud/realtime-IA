# Script para iniciar o LibreTranslate no Docker

# Verificar se Docker está instalado
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker não está instalado ou não está no PATH" -ForegroundColor Red
    exit
}

Write-Host "🚀 Iniciando LibreTranslate..." -ForegroundColor Green

# Usar docker-compose se disponível, senão usar docker direto
if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
    docker-compose up -d
} else {
    docker compose up -d
}

Write-Host "✓ Container iniciado!" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 LibreTranslate disponível em: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para parar o container, execute: docker compose down" -ForegroundColor Yellow
