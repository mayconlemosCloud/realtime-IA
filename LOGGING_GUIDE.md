# 📋 Sistema de Logging - Guia de Uso

## O que foi adicionado

Um sistema de logging completo que captura todos os eventos e erros em um arquivo TXT localizado em:

```
bin/Debug/net9.0-windows/Logs/transacao_YYYY-MM-DD_HH-mm-ss.log
```

## Como usar

### 1️⃣ Executar a aplicação normalmente
```bash
dotnet run
```

### 2️⃣ Reproduzir o erro
- Clique em "Iniciar Transcrição"
- Deixe executar até o erro ocorrer

### 3️⃣ Localizar o arquivo de log

O arquivo será criado automaticamente em:
- **Windows**: `bin\Debug\net9.0-windows\Logs\transacao_*.log`
- **Linux/Mac**: `bin/Debug/net9.0-*/Logs/transacao_*.log`

### 4️⃣ Analisar o erro

Abra o arquivo .log em um editor de texto. Você verá:

```
[2026-02-25 10:15:30.123] [INFO   ] ===== APLICAÇÃO INICIADA =====
[2026-02-25 10:15:30.145] [INFO   ] Carregando variáveis de ambiente (.env)...
[2026-02-25 10:15:30.200] [INFO   ] ===== INICIANDO TRANSCRIÇÃO =====
[2026-02-25 10:15:30.300] [INFO   ] Dispositivo selecionado: Microfone (Realtek High Definition Audio)
[2026-02-25 10:15:30.350] [INFO   ] Opção selecionada: 2
[2026-02-25 10:15:30.400] [ERROR  ] ERRO: NullReferenceException: Object reference not set to an instance of an object
  Exception: NullReferenceException: Object reference not set to an instance of an object
  StackTrace:
   at TraducaoTIME.UIWPF.MainWindow.ShowTranslation(TranscriptionSegment segment) in C:\...\MainWindow.xaml.cs:line XYZ
```

## Tipos de Log

- **INFO**: Eventos normais (aplicação iniciada, transcrição começou, etc)
- **WARNING**: Avisos (configuração faltando, etc)
- **DEBUG**: Detalhes técnicos para diagnóstico
- **ERROR**: Erros com stack trace completo

## Onde verificar erros

### Principais pontos de log:

1. **Inicialização da aplicação**
   ```
   [INFO] ===== APLICAÇÃO INICIADA =====
   ```

2. **Ao clicar em "Iniciar Transcrição"**
   ```
   [INFO] ===== INICIANDO TRANSCRIÇÃO =====
   [INFO] Registrando callbacks de transcrição
   [INFO] Thread de transcrição iniciada
   ```

3. **Durante a transcrição**
   ```
   [DEBUG] [ShowTranslation] Recebido: IsFinal=false, Text='...'
   [DEBUG] [ShowTranslation] Adicionando ao ViewModel
   ```

4. **Erros (procure por ERROR)**
   ```
   [ERROR] ERRO NA THREAD DE TRANSCRIÇÃO: ...
   [ERROR] ERRO GERAL EM BUTTONICIAR: ...
   ```

## Como reportar o problema

1. Abra o arquivo `.log` mais recente
2. Procure por linhas com `[ERROR]`
3. Copie as últimas 50 linhas antes do erro e as 10 linhas depois
4. Cole no relatório de bug

## Exemplo de estrutura de log

```
[2026-02-25 10:15:30.123] [INFO   ] ===== APLICAÇÃO INICIADA =====
[2026-02-25 10:15:30.145] [INFO   ] Carregando variáveis de ambiente (.env)...
[2026-02-25 10:15:30.160] [INFO   ] Variáveis de ambiente carregadas com sucesso
[2026-02-25 10:15:30.200] [INFO   ] Criando aplicação WPF...
[2026-02-25 10:15:30.220] [INFO   ] Criando janela principal...
[2026-02-25 10:15:30.300] [INFO   ] Executando aplicação...
[2026-02-25 10:15:35.400] [INFO   ] ===== INICIANDO TRANSCRIÇÃO =====
[2026-02-25 10:15:35.420] [INFO   ] Dispositivo selecionado: Microfone Padrão
[2026-02-25 10:15:35.430] [INFO   ] Criando thread de transcrição
[2026-02-25 10:15:35.440] [INFO   ] Registrando callbacks de transcrição
[2026-02-25 10:15:35.450] [INFO   ] Iniciando thread
[2026-02-25 10:15:35.460] [INFO   ] Thread iniciada com sucesso
[2026-02-25 10:15:35.500] [INFO   ] Thread de transcrição iniciada
[2026-02-25 10:15:35.510] [INFO   ] Opção selecionada: 2
[2026-02-25 10:15:35.520] [INFO   ] Dispositivo obtido: Microfone Padrão
[2026-02-25 10:15:35.530] [INFO   ] Iniciando Transcrição COM diarização
[2026-02-25 10:15:35.550] [INFO   ] === TranscricaoComDiarizacao.Executar iniciado ===
[2026-02-25 10:15:35.560] [INFO   ] Credenciais encontradas
[2026-02-25 10:15:35.600] [ERROR  ] ERRO: Descrição do erro aqui...
      Exception: TipoDeExcecao: Mensagem detalhada
      StackTrace:
         em TraducaoTIME.Features.TranscricaoComDiarizacao.TranscricaoComDiarizacao.Executar(MMDevice device)
```

## Dicas de troubleshooting

1. **Se o arquivo de log não for criado:**
   - Verifique permissões na pasta `bin/Debug/net9.0-windows/`
   - Tente criar a pasta `Logs` manualmente

2. **Se houver "Acesso negado" ao escrever no log:**
   - Feche outros programas que possam estar usando o arquivo
   - Verifique permissões da pasta

3. **Se não ver nenhum erro no log:**
   - O erro pode estar acontecendo antes da inicialização completa
   - Verifique o console ou capture a saída padrão do `dotnet run`

---

**Agora é possível capturar e analisar os erros em tempo real!** 🎉
