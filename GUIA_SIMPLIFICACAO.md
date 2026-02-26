# 📋 Guia de Simplificação do Código - TraducaoTIME

## 🎯 Objetivo

Simplificar e objetivar o código mantendo **Clean Architecture** e **Princípios SOLID**, tornando o projeto mais manutenível, testável e escalável.

---

## 📊 Estado Atual do Projeto

### ✅ Pontos Positivos

- ✅ Injeção de Dependência bem implementada (Program.cs)
- ✅ Factory Pattern adequadamente aplicado
- ✅ Interfaces abstratas definidas no Core
- ✅ Event-based communication entre camadas
- ✅ Separação de responsabilidades (Core, Services, UI)

### ⚠️ Áreas de Melhoria

| Problema | Severidade | Impacto |
|----------|-----------|--------|
| Código duplicado em serviços de transcrição | 🔴 Alto | Manutenção |
| Logging espalhado + classe estática | 🔴 Alto | Testabilidade |
| AppConfig usando Singleton + Lazy init | 🟡 Médio | DI |
| AIService como Singleton (não injetado) | 🔴 Alto | DI/Testabilidade |
| MainWindow com lógica de coordenação | 🟡 Médio | SRP |
| Modelos espalhados (UIWPF.Models) | 🟡 Médio | Arquitetura |
| Validação de credenciais repetida | 🔴 Alto | DRY |

---

## 🔧 Recomendações de Simplificação

### 1. **Extrair Lógica Comum de Transcrição (DRY)**

#### Problema Atual
Os três serviços (`TranscricaoSemDiarizacaoService`, `TranscricaoComDiarizacaoService`, `CapturaAudioService`) repetem:
- Validação de credenciais Azure
- Teste de conexão HTTP
- Captura de áudio com NAudio
- Tratamento de erros
- Logging

#### Solução: Base Class Abstrata

```csharp
// Services/Transcription/BaseTranscriptionService.cs
public abstract class BaseTranscriptionService : ITranscriptionService
{
    protected readonly IConfigurationService ConfigurationService;
    protected readonly ITranscriptionEventPublisher EventPublisher;
    protected readonly IHistoryManager HistoryManager;
    protected readonly ILogger Logger;
    protected bool ShouldStop = false;

    public abstract string ServiceName { get; }

    protected BaseTranscriptionService(
        IConfigurationService configurationService,
        ITranscriptionEventPublisher eventPublisher,
        IHistoryManager historyManager,
        ILogger logger)
    {
        ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        HistoryManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Método protegido - usado por todas as implementações
    protected async Task<(bool Success, string ErrorMessage)> ValidateAzureCredentialsAsync()
    {
        string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
        string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

        if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
        {
            return (false, "❌ ERRO: Variáveis de ambiente não configuradas!");
        }

        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
                var testUrl = $"https://{azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
                var response = await httpClient.PostAsync(testUrl, new StringContent(""));
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = response.StatusCode.ToString();
                    string erro = response.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                        ? "❌ ERRO: Chave API inválida!" 
                        : response.StatusCode == System.Net.HttpStatusCode.Forbidden 
                        ? "❌ ERRO: Quota foi excedida!" 
                        : $"❌ ERRO: {errorMsg}";
                    return (false, erro);
                }
            }
            Logger.Info($"[{ServiceName}] Autenticação Azure validada");
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"❌ ERRO DE CONEXÃO: {ex.Message}");
        }
    }

    // Template Method Pattern
    public abstract Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default);
    
    public virtual void Stop()
    {
        Logger.Info($"[{ServiceName}] Parando...");
        ShouldStop = true;
    }

    // Helper protegido
    protected IWaveIn CreateWaveCapture(MMDevice device)
    {
        IWaveIn capture = device.DataFlow == DataFlow.Render
            ? new WasapiLoopbackCapture(device)
            : new WasapiCapture(device);

        capture.WaveFormat = new WaveFormat(16000, 16, 1);
        return capture;
    }
}
```

#### Benefícios
- ✅ Reduz duplicação em ~40%
- ✅ Manutenção centralizada
- ✅ Implementações mais limpas e focadas
- ✅ Template Method Pattern

---

### 2. **Injetar Logger como Dependência (Remover Static)**

#### Problema Atual
```csharp
// ❌ Classe Static - Difícil de testar, difícil de mockar
public static class Logger
{
    public static void Info(string message) { ... }
    public static void Error(string message, Exception? ex = null) { ... }
}

// Uso espalhado
Logger.Error("Erro", ex);
```

#### Solução: Já Implementada Parcialmente!

```csharp
// Core/Abstractions/ILogger.cs - JÁ EXISTE
public interface ILogger
{
    void Info(string message);
    void Error(string message, Exception? ex = null);
    void Warning(string message);
    void Debug(string message);
}

// Services/Logging/LoggerProvider.cs (MELHORADA)
public class LoggerProvider : ILogger
{
    private readonly ILoggerOutput _output;

    public LoggerProvider(ILoggerOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Info(string message) => _output.Write("INFO", message);
    public void Error(string message, Exception? ex = null) => 
        _output.Write("ERROR", ex != null ? $"{message}\n{ex}" : message);
    public void Warning(string message) => _output.Write("WARNING", message);
    public void Debug(string message) => _output.Write("DEBUG", message);
}

// Strategy Pattern para saída do log
public interface ILoggerOutput
{
    void Write(string level, string message);
}

public class FileLoggerOutput : ILoggerOutput
{
    private readonly string _logPath;
    private readonly object _lock = new object();

    public FileLoggerOutput(string logPath)
    {
        _logPath = logPath;
    }

    public void Write(string level, string message)
    {
        lock (_lock)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.PadRight(7)}] {message}";
            File.AppendAllText(_logPath, logMessage + "\n");
            Console.WriteLine(logMessage);
        }
    }
}

// Program.cs - Configurar DI
services.AddSingleton<ILoggerOutput>(sp => 
{
    string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    Directory.CreateDirectory(logFolder);
    string logPath = Path.Combine(logFolder, $"transacao_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
    return new FileLoggerOutput(logPath);
});
services.AddSingleton<ILogger, LoggerProvider>();
```

#### Mudança de Uso
```csharp
// ✅ Agora via Injeção
private readonly ILogger _logger;

public MyService(ILogger logger)
{
    _logger = logger;
}

public void DoSomething()
{
    _logger.Info("Fazer algo");  // Injetado, testável, mocável
}
```

#### Benefícios
- ✅ Totalmente testável e mocável
- ✅ Suporta múltiplas saídas (File, Console, Cloud)
- ✅ Strategy Pattern para extensibilidade
- ✅ Remove dependência de Static

---

### 3. **Converter AIService para Injeção de Dependência**

#### Problema Atual
```csharp
// ❌ Singleton estático - Não é injetável
public class AIService
{
    private static AIService? _instance;

    public static AIService Instance
    {
        get
        {
            _instance ??= new AIService();
            return _instance;
        }
    }
}

// Uso
AIService.Instance.AnalyzeConversation(...);
```

#### Solução: Interface + DI

```csharp
// Core/Abstractions/IAIService.cs
public interface IAIService
{
    string AnalyzeConversationWithRAG(string question, string conversationHistory);
    string ExtractKeywords(string text);
    string GenerateResponse(string question, string context);
}

// Services/AI/OpenAIService.cs ou LocalAIService.cs
public class OpenAIService : IAIService
{
    private readonly ILogger _logger;
    private readonly IConfigurationService _config;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiProvider;

    public OpenAIService(
        ILogger logger,
        IConfigurationService config,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config;
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? "";
        _apiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "local";
    }

    public string AnalyzeConversationWithRAG(string question, string conversationHistory)
    {
        var relevantContext = ExtractKeywords(question);
        return GenerateResponse(question, relevantContext);
    }

    public string ExtractKeywords(string text) { /* ... */ }
    public string GenerateResponse(string question, string context) { /* ... */ }
}

// Program.cs - Injetar
services.AddHttpClient<IAIService, OpenAIService>();
// Ou
services.AddSingleton<IAIService, LocalAIService>();
```

#### Benefícios
- ✅ Totalmente testável
- ✅ Fácil trocar implementações (Local ↔ OpenAI)
- ✅ Suporta mocking
- ✅ Segue SOLID

---

### 4. **Consolidar Modelos (Core.Models)**

#### Problema Atual
```
UIWPF/Models/
  └─ FinalizedLineItem.cs (Modelo UI)

Core/Models/
  └─ TranscriptionSegment.cs (Modelo Domínio)
```
Modelos espalhados em diversos places.

#### Solução: Organização Clara

```
Core/
  ├─ Models/
  │   ├─ TranscriptionSegment.cs
  │   ├─ TranscriptionResult.cs
  │   ├─ HistoryEntry.cs
  │   └─ AudioDevice.cs (wrapper de MMDevice)
  │
  └─ Abstractions/
      ├─ ITranscriptionService.cs
      ├─ IAIService.cs
      └─ ...

UIWPF/
  ├─ ViewModels/
  │   ├─ MainWindowViewModel.cs
  │   └─ ConfigWindowViewModel.cs (se existir)
  │
  ├─ Models/
  │   └─ DisplayModels/
  │       └─ FinalizedLineItem.cs (Mapeamento UI)
  │
  └─ Converters/
      └─ BoolToVisibilityConverter.cs
```

**Mapeamento de Modelos:**
```csharp
// Core always provides domain models
TranscriptionSegment (domínio)
        ↓ [Mapeamento automático]
FinalizedLineItem (apresentação)
```

#### Benefícios
- ✅ Separação clara Domínio ↔ Apresentação
- ✅ Core não depende de UI
- ✅ Modelos reutilizáveis
- ✅ Fácil de testar

---

### 5. **Remover Duplicação de Debug.WriteLine()**

#### Problema Atual
```csharp
// ❌ Espalhado em vários arquivos
System.Diagnostics.Debug.WriteLine($"[ViewModel] AddFinalizedLine: ...");
System.Diagnostics.Debug.WriteLine($"[ViewModel] Item criado...");
_logger.Info(...);  // Duplicado!
```

#### Solução: Usar Apenas ILogger

```csharp
// ✅ Centralizar em ILogger
_logger.Debug("AddFinalizedLine chamado");
_logger.Debug($"Item criado: {item.DisplayText}");

// Remove Debug.WriteLine completamente
// Se precisar de debug, ativa log level Debug
```

**Program.cs - Controlar nível de log:**
```csharp
string logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "info";

services.AddSingleton<ILogger>(sp =>
{
    var output = sp.GetRequiredService<ILoggerOutput>();
    return new LoggerProvider(output, logLevel);
});
```

#### Benefícios
- ✅ Código mais limpo
- ✅ Log centralizado
- ✅ Fácil controlar nível via env var
- ✅ Remove 100+ linhas desnecessárias

---

### 6. **Simplificar ConfigWindow e AppConfig**

#### Problema Atual
```csharp
// AppConfig.cs - Ainda usa Singleton Pattern
public static AppConfig Instance
{
    get
    {
        _instance ??= new AppConfig();
        return _instance;
    }
}

// Misto: DI + Singleton antipadrão
```

#### Solução: Puro DI

```csharp
// Core/Abstractions/IConfigurationService.cs (MELHORADA)
public interface IConfigurationService
{
    string SelectedOption { get; set; }
    string? SelectedDeviceName { get; set; }
    MAudioDevice? SelectedDevice { get; }
    IEnumerable<AudioDevice> AvailableDevices { get; }
    bool IsValid();

    event EventHandler? ConfigurationChanged;
}

// Core/Models/AudioDevice.cs (NOVO - Wrapper)
public class AudioDevice
{
    public string FriendlyName { get; set; }
    public MMDevice NativeDevice { get; set; }
}

// Services/Configuration/AppConfig.cs (SIMPLIFICADO)
public class AppConfig : IConfigurationService
{
    private string _selectedOption = "1";
    private string _selectedDeviceName = "";
    private readonly IAudioDeviceProvider _deviceProvider;

    public event EventHandler? ConfigurationChanged;

    public AppConfig(IAudioDeviceProvider deviceProvider)
    {
        _deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
    }

    public string SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (_selectedOption != value)
            {
                _selectedOption = value;
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public IEnumerable<AudioDevice> AvailableDevices => _deviceProvider.GetAvailableDevices();

    public AudioDevice? SelectedDevice => 
        AvailableDevices.FirstOrDefault(d => d.FriendlyName == _selectedDeviceName);

    public bool IsValid() => 
        !string.IsNullOrWhiteSpace(_selectedDeviceName) && SelectedDevice != null;
}

// Program.cs - Simples
services.AddSingleton<IAudioDeviceProvider, NAudioDeviceProvider>();
services.AddSingleton<IConfigurationService, AppConfig>();
```

#### Benefícios
- ✅ Remove Singleton Pattern
- ✅ 100% injetável
- ✅ Testável e mocável
- ✅ Mais limpo

---

### 7. **Simplificar MainWindow - Extrair Coordenação**

#### Problema Atual
```csharp
// MainWindow.xaml.cs - ~280 linhas
// Responsabilidades:
// 1. Renderizar UI
// 2. Coordenar transcrição
// 3. Manipular histórico
// 4. Tratar erros
// 5. Atualizar status
```

#### Solução: Coordinator Pattern

```csharp
// Services/TranscriptionCoordinator.cs (NOVO)
public interface ITranscriptionCoordinator
{
    Task StartTranscriptionAsync(CancellationToken cancellationToken);
    void StopTranscription();
    bool IsRunning { get; }
}

public class TranscriptionCoordinator : ITranscriptionCoordinator
{
    private readonly ITranscriptionEventPublisher _eventPublisher;
    private readonly IHistoryManager _historyManager;
    private readonly IConfigurationService _configuration;
    private readonly TranscriptionServiceFactory _factory;
    private readonly ILogger _logger;

    private ITranscriptionService? _currentService;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _currentService != null;

    public TranscriptionCoordinator(
        ITranscriptionEventPublisher eventPublisher,
        IHistoryManager historyManager,
        IConfigurationService configuration,
        TranscriptionServiceFactory factory,
        ILogger logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartTranscriptionAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info("=== INICIANDO TRANSCRIÇÃO ===");

            if (!_configuration.IsValid())
            {
                throw new InvalidOperationException("Dispositivo não configurado");
            }

            _historyManager.Clear();

            var device = _configuration.SelectedDevice;
            var option = _configuration.SelectedOption;

            _currentService = _factory.CreateService(option);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var result = await _currentService.StartAsync(device.NativeDevice, _cts.Token);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Erro ao iniciar transcrição", ex);
            _eventPublisher.OnErrorOccurred(ex);
            throw;
        }
        finally
        {
            _currentService = null;
            _cts?.Dispose();
        }
    }

    public void StopTranscription()
    {
        _logger.Info("Parando transcrição...");
        _currentService?.Stop();
        _cts?.Cancel();
    }
}

// UIWPF/MainWindow.xaml.cs (MUITO MAIS SIMPLES)
public partial class MainWindow : Window
{
    private readonly ITranscriptionCoordinator _coordinator;
    private readonly ITranscriptionEventPublisher _eventPublisher;
    private readonly MainWindowViewModel _viewModel;
    private readonly ILogger _logger;

    public MainWindow(
        ITranscriptionCoordinator coordinator,
        ITranscriptionEventPublisher eventPublisher,
        MainWindowViewModel viewModel,
        ILogger logger)
    {
        InitializeComponent();
        
        _coordinator = coordinator;
        _eventPublisher = eventPublisher;
        _viewModel = viewModel;
        _logger = logger;

        this.DataContext = _viewModel;

        _eventPublisher.SegmentReceived += OnSegmentReceived;
        _eventPublisher.ErrorOccurred += OnErrorOccurred;
        _eventPublisher.TranscriptionStarted += OnTranscriptionStarted;
        _eventPublisher.TranscriptionCompleted += OnTranscriptionCompleted;

        _logger.Info("MainWindow inicializada");
    }

    private async void ButtonIniciar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            buttonIniciar.IsEnabled = false;
            buttonParar.IsEnabled = true;

            await _coordinator.StartTranscriptionAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            buttonIniciar.IsEnabled = true;
            buttonParar.IsEnabled = false;
        }
    }

    private void ButtonParar_Click(object sender, RoutedEventArgs e)
    {
        _coordinator.StopTranscription();
    }

    private void OnSegmentReceived(object? sender, TranscriptionSegmentReceivedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var segment = e.Segment;

            if (segment.IsFinal && !string.IsNullOrWhiteSpace(segment.Text))
            {
                string speaker = segment.Speaker ?? "Participante";
                _viewModel.AddFinalizedLine(segment.Text, speaker);
            }
            else if (!segment.IsFinal)
            {
                _viewModel.CurrentInterimText = 
                    !string.IsNullOrWhiteSpace(segment.Speaker)
                    ? $"{segment.Speaker}: {segment.Text}"
                    : segment.Text;
            }
        });
    }

    private void OnErrorOccurred(object? sender, TranscriptionErrorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                $"Erro: {e.Exception?.Message}",
                "Erro na Transcrição",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }

    private void OnTranscriptionStarted(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.ClearAllLines();
            buttonIniciar.IsEnabled = false;
            buttonParar.IsEnabled = true;
        });
    }

    private void OnTranscriptionCompleted(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            buttonIniciar.IsEnabled = true;
            buttonParar.IsEnabled = false;
        });
    }
}

// Program.cs
services.AddSingleton<ITranscriptionCoordinator, TranscriptionCoordinator>();
services.AddSingleton<MainWindow>();
```

#### Benefícios
- ✅ MainWindow reduzida de 280 → ~140 linhas
- ✅ Lógica de coordenação testável
- ✅ UI apenas com apresentação
- ✅ Single Responsibility

---

### 8. **Simplificar HistoryManager**

#### Problema Atual
```csharp
// Guarda histórico em memória + arquivo
// Lógica de I/O espalhada
private List<HistoryEntry> _entries = new List<HistoryEntry>();
private string _historyFilePath;
private readonly object _fileLock = new object();
```

#### Solução: Separar Responsabilidades

```csharp
// Core/Abstractions/IHistoryStorage.cs (NOVO)
public interface IHistoryStorage
{
    Task SaveAsync(HistoryEntry entry);
    Task<IEnumerable<HistoryEntry>> LoadAsync();
}

// Services/History/FileHistoryStorage.cs
public class FileHistoryStorage : IHistoryStorage
{
    private readonly string _historyPath;
    private readonly object _lock = new object();

    public FileHistoryStorage(string historyPath)
    {
        _historyPath = historyPath ?? throw new ArgumentNullException(nameof(historyPath));
        InitializeFile();
    }

    public async Task SaveAsync(HistoryEntry entry)
    {
        lock (_lock)
        {
            string line = $"[{entry.Timestamp:HH:mm:ss}] {entry.Speaker}: {entry.Text}";
            File.AppendAllText(_historyPath, line + "\n");
        }
    }

    public async Task<IEnumerable<HistoryEntry>> LoadAsync()
    {
        if (!File.Exists(_historyPath))
            return new List<HistoryEntry>();

        var entries = new List<HistoryEntry>();
        // Parsear e reconstruir
        return entries;
    }

    private void InitializeFile()
    {
        lock (_lock)
        {
            if (!File.Exists(_historyPath))
            {
                File.WriteAllText(_historyPath, $"=== Histórico iniciado em {DateTime.Now}\n");
            }
        }
    }
}

// Core/Abstractions/IHistoryManager.cs (REFATORADA)
public interface IHistoryManager
{
    void Clear();
    void AddMessage(string speaker, string text);
    IEnumerable<HistoryEntry> GetHistory();
}

// Services/History/HistoryManager.cs (SIMPLIFICADA)
public class HistoryManager : IHistoryManager
{
    private readonly List<HistoryEntry> _entries = new();
    private readonly IHistoryStorage _storage;
    private readonly ILogger _logger;

    public HistoryManager(IHistoryStorage storage, ILogger logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Clear()
    {
        _entries.Clear();
    }

    public void AddMessage(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var entry = new HistoryEntry
        {
            Speaker = speaker,
            Text = text,
            Timestamp = DateTime.Now
        };

        _entries.Add(entry);

        // Fire and forget - não bloqueia
        _ = _storage.SaveAsync(entry);
    }

    public IEnumerable<HistoryEntry> GetHistory() => _entries.AsReadOnly();
}

// Program.cs
services.AddSingleton<IHistoryStorage>(sp =>
{
    string historyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TraducaoTIME",
        "Historico",
        $"conversa_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
    return new FileHistoryStorage(historyPath);
});
services.AddSingleton<IHistoryManager, HistoryManager>();
```

#### Benefícios
- ✅ Separação clara: In-Memory vs Persistência
- ✅ Fácil mockar storage para testes
- ✅ ~100 linhas → ~70 linhas
- ✅ Async I/O não bloqueia UI

---

### 9. **Consolidar Configurações**

#### Problema Atual
```csharp
// Variáveis de ambiente lidas em múltiplos lugares
Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");  // Em 3 serviços
Environment.GetEnvironmentVariable("AI_API_KEY");        // Em AIService
Environment.GetEnvironmentVariable("AI_PROVIDER");       // Em AIService
```

#### Solução: AppSettings Centralizado

```csharp
// Core/Models/AppSettings.cs (NOVO)
public class AppSettings
{
    public AzureSettings Azure { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class AzureSettings
{
    public string SpeechKey { get; set; } = "";
    public string SpeechRegion { get; set; } = "";
}

public class AISettings
{
    public string ApiKey { get; set; } = "";
    public string Provider { get; set; } = "local";
}

public class LoggingSettings
{
    public string Level { get; set; } = "info";
    public string OutputPath { get; set; } = "Logs";
}

// Program.cs - Carregar de env vars
var appSettings = new AppSettings
{
    Azure = new AzureSettings
    {
        SpeechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
        SpeechRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? ""
    },
    AI = new AISettings
    {
        ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? "",
        Provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "local"
    },
    Logging = new LoggingSettings
    {
        Level = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "info",
        OutputPath = Environment.GetEnvironmentVariable("LOG_PATH") ?? "Logs"
    }
};

services.AddSingleton(appSettings);

// Nos serviços - Usar injeção
public class TranscricaoSemDiarizacaoService : BaseTranscriptionService
{
    private readonly AppSettings _settings;

    public TranscricaoSemDiarizacaoService(
        IConfigurationService configurationService,
        ITranscriptionEventPublisher eventPublisher,
        IHistoryManager historyManager,
        ILogger logger,
        AppSettings settings) : base(configurationService, eventPublisher, historyManager, logger)
    {
        _settings = settings;
    }

    public override async Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default)
    {
        var azureKey = _settings.Azure.SpeechKey;
        var azureRegion = _settings.Azure.SpeechRegion;
        // ...
    }
}
```

#### Benefícios
- ✅ Configurações centralizadas
- ✅ Fácil adicionar novos settings
- ✅ Type-safe (vs strings)
- ✅ Remove 50+ linhas de leitura repetida

---

## 📋 Checklist de Implementação

### Fase 1: Foundation (1-2 dias)
- [ ] Criar `BaseTranscriptionService` abstrata
- [ ] Implementar `ILoggerOutput` e converter `Logger` estático
- [ ] Remover todos `System.Diagnostics.Debug.WriteLine()`
- [ ] Consolidar `AppSettings`

### Fase 2: Refactor Central (2-3 dias)
- [ ] Converter `AIService` para DI
- [ ] Extrair `TranscriptionCoordinator`
- [ ] Simplificar `MainWindow` (~50% redução)
- [ ] Refactor `HistoryManager` Com `IHistoryStorage`

### Fase 3: Polish (1 dia)
- [ ] Reorganizar modelos em `Core/Models`
- [ ] Atualizar `Program.cs` com novos registros
- [ ] Testes unitários para novos componentes
- [ ] Documentação atualizada

---

## ⚙️ Estatísticas de Impacto

| Mudança | Redução | Manutenibilidade | Testabilidade |
|---------|---------|------------------|---------------|
| Remover logging duplicado | 50-80 linhas | ⬆️⬆️ | ⬆️⬆️ |
| BaseTranscriptionService | 200-300 linhas | ⬆️⬆️⬆️ | ⬆️⬆️ |
| Converter para DI | 150-200 linhas | ⬆️⬆️⬆️ | ⬆️⬆️⬆️ |
| TranscriptionCoordinator | 140 linhas savings | ⬆️⬆️ | ⬆️⬆️ |
| AppSettings centralizado | 60-80 linhas | ⬆️⬆️ | ⬆️ |
| **TOTAL** | **600-760 linhas** | **⬆️⬆️⬆️** | **⬆️⬆️⬆️** |

---

## 🎯 Princípios SOLID Mantidos

| Princípio | Antes | Depois | Status |
|-----------|-------|--------|--------|
| **S**ingle Responsibility | MainWindow (280 linhas) | MainWindow (140) + Coordinator | ✅ |
| **O**pen/Closed | Factory + Hardcoded options | BaseTranscriptionService | ✅ |
| **L**iskov Substitution | Interfaces OK | BaseTranscriptionService + Strategy | ✅ |
| **I**nterface Segregation | Services com muitas responsabilidades | Separated concerns | ✅ |
| **D**ependency Inversion | Partial (ainda há Singletons) | 100% DI Container | ✅ |

---

## 📚 Padrões de Design Utilizados

- ✅ **Factory Pattern** - TranscriptionServiceFactory
- ✅ **Strategy Pattern** - ITranscriptionService implementations
- ✅ **Template Method** - BaseTranscriptionService
- ✅ **Observer Pattern** - ITranscriptionEventPublisher
- ✅ **Coordinator Pattern** - TranscriptionCoordinator
- ✅ **Dependency Injection** - ServiceCollection
- ✅ **Repository Pattern** - IHistoryStorage

---

## 🔍 Clean Architecture Compliance

```
┌─────────────────────────────────────────────┐
│         CLEAN ARCHITECTURE LAYERS           │
├─────────────────────────────────────────────┤
│ Entities (Core/Models)       - ✅ OK         │
│ Use Cases (Services)         - ✅ Melhorado  │
│ Interface Adapters (UIWPF)   - ✅ Melhorado  │
│ Frameworks & Drivers (DI)    - ✅ OK         │
└─────────────────────────────────────────────┘
```

**Dependências SEMPRE apontam para dentro** ↓
```
UIWPF → Services → Core
Core ← Services ← UIWPF (❌ Nunca)
```

---

## 🚀 Próximos Passos

1. **Começar pela Fase 1**: Base sólida
2. **Incremental**: Um componente por vez
3. **Testes**: Adicionar testes para componentes novos
4. **Documentação**: Atualizar arquitetura visual
5. **Review**: Code review antes de merge

---

## 📖 Referências

- **Clean Architecture** - Robert C. Martin
- **SOLID Principles** - Robert C. Martin
- **Design Patterns** - Gang of Four
- **Dependency Injection in .NET** - Mark Seemann
- **Microsofts DI Container** - Microsoft Docs

