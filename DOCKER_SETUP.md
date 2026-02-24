# LibreTranslate - Setup Local

## 📋 Pré-requisitos
- Docker Desktop instalado: https://www.docker.com/products/docker-desktop/

## 🚀 Iniciar o Container

### Opção 1: PowerShell (Recomendado)
```powershell
# Execute como Administrador
.\start-docker.ps1
```

### Opção 2: Linha de comando
```bash
docker compose up -d
```

## 📊 Verificar Status
```bash
docker compose ps
# ou
docker ps
```

## 🌐 Testar a API
Abra no navegador: http://localhost:5000

Ou teste com curl:
```bash
curl -X POST "http://localhost:5000/translate" \
  -H "Content-Type: application/json" \
  -d '{"q":"Hello world","source":"en","target":"pt"}'
```

## ⏹️ Parar o Container
```bash
docker compose down
```

## 📦 Informações da Imagem
- **Tamanho**: ~2.5GB (primeira execução)
- **Modelos**: Português (PT) e Inglês (EN) apenas
- **Porta**: 5000
- **Latência**: ~100-200ms na primeira chamada, ~50-100ms após aquecimento

## 💡 Dicas
- O container salva os modelos em volume, não precisa baixar novamente
- A primeira inicialização pode levar 2-3 minutos para baixar os modelos
- Ideal para desenvolvimento local: totalmente grátis e sem dependências de API
