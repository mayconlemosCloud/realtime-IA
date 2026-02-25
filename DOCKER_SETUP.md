# Setup Docker - Ollama + TraducaoTIME

## 📋 Pré-requisitos

- Docker instalado ([Download](https://www.docker.com/products/docker-desktop))
- Docker Compose v2+
- Windows PowerShell ou WSL2 (recomendado para Windows)

## 🚀 Iniciando Ollama via Docker Compose

### 1. Iniciar apenas Ollama (mais rápido)

```bash
docker-compose up -d ollama
```

Isto irá:
- ✅ Baixar a imagem do Ollama (~7GB)
- ✅ Criar container `ollama-ia`
- ✅ Expor na porta `11434`
- ✅ Armazenar dados em volume persistente

### 2. Aguardar Ollama ficar pronto

```bash
# Verificar status
docker-compose ps

# Verificar logs
docker-compose logs -f ollama
```

O Ollama está pronto quando ver:
```
ollama-ia  | Listening on 127.0.0.1:11434
```

### 3. Puxar um modelo (primeira vez é demorado)

```bash
# Entrar no container
docker-compose exec ollama ollama pull llama2

# Ou outras opções:
docker-compose exec ollama ollama pull mistal
docker-compose exec ollama ollama pull neural-chat
```

**Primeira execução**: Pode levar 5-15 minutos (download de 4-10GB)

### 4. Testar Ollama

```powershell
# Windows PowerShell
Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method Get

# Se funcionar, verá JSON com modelos disponíveis
```

Ou via cURL:
```bash
curl http://localhost:11434/api/tags
```

---

## 🌐 Interface Web Opcional (Open WebUI)

Para usar uma interface web bonitinha para testar Ollama:

```bash
# Iniciar com WebUI
docker-compose --profile webui up -d

# Acessar em http://localhost:8080
```

---

## 🔗 Configuração no seu Aplicativo

No arquivo `.env`:

```env
# Ativar Ollama
AI_PROVIDER=ollama
OLLAMA_API_URL=http://localhost:11434
OLLAMA_MODEL=llama2
# ou
OLLAMA_MODEL=mistral
OLLAMA_MODEL=neural-chat
OLLAMA_MODEL=orca-mini
```

---

## 📊 Modelos Recomendados

| Modelo | Tamanho | Velocidade | Qualidade | Comando |
|--------|--------|-----------|----------|---------|
| **orca-mini** | 2GB | ⚡⚡⚡ Rápido | ⭐⭐ | `ollama pull orca-mini` |
| **neural-chat** | 5GB | ⚡⚡ Médio | ⭐⭐⭐ | `ollama pull neural-chat` |
| **mistral** | 4GB | ⚡⚡ Médio | ⭐⭐⭐⭐ | `ollama pull mistral` |
| **llama2** | 4GB | ⚡ Lento | ⭐⭐⭐⭐ | `ollama pull llama2` |

**Recomendação**: Comece com `neural-chat` ou `mistral`

---

## 🛑 Parando e Limpando

```bash
# Parar containers (mantém dados)
docker-compose down

# Parar e remover volumes (deleta tudo)
docker-compose down -v

# Parar apenas Ollama
docker-compose stop ollama
```

---

## 🔍 Troubleshooting

### Ollama não consegue conectar

```bash
# Verificar se está rodando
docker-compose ps

# Verificar logs
docker-compose logs ollama

# Reiniciar
docker-compose restart ollama
```

### Porta 11434 já está em uso

Mude a porta no `docker-compose.yml`:
```yaml
ports:
  - "11435:11434"  # Use 11435 ao invés de 11434
```

### Sem espaço em disco (modelos são grandes)

```bash
# Listar espaço
docker system df

# Limpar cache Docker
docker system prune -a
```

### Container sai com erro

```bash
# Ver erro detalhado
docker-compose logs ollama --tail 50

# Tentar recriar
docker-compose down
docker-compose up --build ollama
```

---

## 💡 Testes Rápidos

### Teste via PowerShell

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:11434/api/generate" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"} `
  -Body '{"model":"llama2","prompt":"Olá, como você está?","stream":false}' `
  -UseBasicParsing

$response.Content | ConvertFrom-Json | Select -ExpandProperty response
```

### Teste via cURL (WSL/Git Bash)

```bash
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"llama2","prompt":"Olá, como você está?","stream":false}' \
  | jq '.response'
```

---

## 🎯 Próximos Passos

1. ✅ Iniciar Ollama com `docker-compose up -d ollama`
2. ✅ Puxar um modelo com `docker-compose exec ollama ollama pull mistral`
3. ✅ Configurar `.env` com `AI_PROVIDER=ollama`
4. ✅ Executar a aplicação
5. ✅ Usar o menu "IA" para fazer perguntas sobre a conversa

---

## 📚 Recursos

- [Ollama Docs](https://github.com/ollama/ollama)
- [Open WebUI](https://github.com/open-webui/open-webui)
- [Modelos Disponíveis](https://ollama.ai/library)
