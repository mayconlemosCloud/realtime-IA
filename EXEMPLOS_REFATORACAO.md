# 🔨 Exemplos Práticos de Refatoração

## 1. Criar Interfaces Abstratas

### ITranscriptionService.cs
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Core.Abstractions
{
    /// <summary>
    /// Interface abstrata para qualquer serviço de transcrição
    /// Permite trocar implementações sem alterar código-cliente
    /// </summary>
    public interface ITranscriptionService
    {
        /// <summary>
        /// Inicia a transcrição de áudio
        /// </summary>
        Task<TranscriptionResult> StartAsync(
            AudioDevice device, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Para a transcrição em andamento
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Nome amigável do serviço (para debug/logs)
        /// </summary>
        string ServiceName { get; }
    }
    
    public class TranscriptionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalSegments { get; set; }
    }
}
```

### ITranscriptionEventPublisher.cs
```csharp
using System;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Core.Abstractions
{
    /// <summary>
    /// Publica eventos de transcrição de forma desacoplada
    /// Em vez de callbacks estáticos
    /// </summary>
    public interface ITranscriptionEventPublisher
    {
        /// <summary>
        /// Disparado quando um segmento de transcrição é recebido
        /// </summary>
        event EventHandler<TranscriptionSegmentReceivedEventArgs>? SegmentReceived;
        
        /// <summary>
        /// Disparado quando ocorre um erro
        /// </summary>
        event EventHandler<TranscriptionErrorEventArgs>? ErrorOccurred;
        
        /// <summary>
        /// Disparado quando transcrição começa
        /// </summary>
        event EventHandler<EventArgs>? TranscriptionStarted;
        
        /// <summary>
        /// Disparado quando transcrição termina
        /// </summary>
        event EventHandler<EventArgs>? TranscriptionCompleted;
    }
    
    public class TranscriptionSegmentReceivedEventArgs : EventArgs
    {
        public required TranscriptionSegment Segment { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
    }
    
    public class TranscriptionErrorEventArgs : EventArgs
    {
        public required Exception Exception { get; set; }
        public string? SegmentContext { get; set; }
    }
}
```

### IHistoryManager.cs
```csharp
using System;
using System.Collections.Generic;

namespace TraducaoTIME.Core.Abstractions
{
    public interface IHistoryManager
    {
        /// <summary>
        /// Adiciona uma mensagem ao histórico
        /// </summary>
        void AddMessage(string speaker, string text);
        
        /// <summary>
        /// Retorna todo o histórico
        /// </summary>
        IEnumerable<HistoryEntry> GetHistory();
        
        /// <summary>
        /// Retorna histórico formatado como texto
        /// </summary>
        string GetFormattedHistory();
        
        /// <summary>
        /// Limpa o histórico
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Número de entradas no histórico
        /// </summary>
        int Count { get; }
    }
    
    public class HistoryEntry
    {
        public string Speaker { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

### ILogger.cs
```csharp
using System;

namespace TraducaoTIME.Core.Abstractions
{
    /// <summary>
    /// Interface centralizada para logging
    /// Elimina duplicação de Logger.Instance + Debug.WriteLine
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
    }
}
```

### IConfigurationService.cs
```csharp
using System;
using NAudio.CoreAudioApi;

namespace TraducaoTIME.Core.Abstractions
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Opção de transcrição selecionada (1, 2 ou 3)
        /// </summary>
        string SelectedOption { get; set; }
        
        /// <summary>
        /// Dispositivo de áudio selecionado
        /// </summary>
        MMDevice? SelectedDevice { get; }
        
        /// <summary>
        /// Nome amigável do dispositivo
        /// </summary>
        string? SelectedDeviceName { get; set; }
        
        /// <summary>
        /// Evento disparado quando configuração muda
        /// </summary>
        event EventHandler? ConfigurationChanged;
        
        /// <summary>
        /// Valida se configuração está pronta
        /// </summary>
        bool IsValid();
    }
}
```

---

## 2. Implementar Event Publisher

### TranscriptionEventPublisher.cs
```csharp
using System;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Utils;

namespace TraducaoTIME.Services.Events
{
    /// <summary>
    /// Publicador centralizado de eventos de transcrição
    /// Substitui callbacks estáticos espalhados pelo código
    /// </summary>
    public class TranscriptionEventPublisher : ITranscriptionEventPublisher
    {
        private readonly ILogger _logger;
        
        public event EventHandler<TranscriptionSegmentReceivedEventArgs>? SegmentReceived;
        public event EventHandler<TranscriptionErrorEventArgs>? ErrorOccurred;
        public event EventHandler<EventArgs>? TranscriptionStarted;
        public event EventHandler<EventArgs>? TranscriptionCompleted;
        
        public TranscriptionEventPublisher(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public void OnSegmentReceived(TranscriptionSegment segment)
        {
            _logger.Debug($"[EventPublisher] Publicando SegmentReceived: IsFinal={segment.IsFinal}");
            SegmentReceived?.Invoke(this, new TranscriptionSegmentReceivedEventArgs 
            { 
                Segment = segment 
            });
        }
        
        public void OnErrorOccurred(Exception exception, string? context = null)
        {
            _logger.Error($"[EventPublisher] Publicando ErrorOccurred", exception);
            ErrorOccurred?.Invoke(this, new TranscriptionErrorEventArgs 
            { 
                Exception = exception,
                SegmentContext = context
            });
        }
        
        public void OnTranscriptionStarted()
        {
            _logger.Info("[EventPublisher] Transcrição iniciada");
            TranscriptionStarted?.Invoke(this, EventArgs.Empty);
        }
        
        public void OnTranscriptionCompleted()
        {
            _logger.Info("[EventPublisher] Transcrição concluída");
            TranscriptionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
```

---

## 3. Factory Pattern para Serviços

### TranscriptionServiceFactory.cs
```csharp
using System;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services.Transcription;

namespace TraducaoTIME.Services
{
    /// <summary>
    /// Factory para criar serviços de transcrição
    /// Centraliza lógica de criação, permite adicionar novas implementações sem alterar MainWindow
    /// </summary>
    public class TranscriptionServiceFactory
    {
        private readonly IConfigurationService _configurationService;
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly ILogger _logger;
        
        public TranscriptionServiceFactory(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Cria um serviço baseado na opção
        /// OPEN/CLOSED: Adicione novos serviços sem modificar este método
        /// </summary>
        public ITranscriptionService CreateService(string option)
        {
            _logger.Info($"[Factory] Criando serviço para opção: {option}");
            
            return option switch
            {
                "1" => new TranscricaoSemDiarizacaoService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger),
                    
                "2" => new TranscricaoComDiarizacaoService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger),
                    
                "3" => new CapturaAudioService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger),
                    
                _ => throw new InvalidOperationException($"Opção de transcrição desconhecida: {option}")
            };
        }
        
        /// <summary>
        /// Para adicionar novo serviço:
        /// 1. Criar class NovoServicoTranscricao : ITranscriptionService
        /// 2. Adicionar novo case aqui
        /// 3. Nenhuma alteração em MainWindow necessária!
        /// </summary>
        public ITranscriptionService CreateCustomService(string serviceType)
        {
            _logger.Warning($"[Factory] Tentativa de criar serviço customizado: {serviceType}");
            throw new NotImplementedException($"Serviço customizado '{serviceType}' não implementado");
        }
    }
}
```

---

## 4. Refatorar MainWindow para Usar DI

### MainWindow.xaml.cs (Refatorado)
```csharp
using System;
using System.Windows;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services;
using TraducaoTIME.UIWPF.ViewModels;

namespace TraducaoTIME.UIWPF
{
    /// <summary>
    /// MainWindow Refatorada
    /// Agora é THIN CODE-BEHIND, apenas coordena apresentação
    /// Toda lógica injetada via DI
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly IConfigurationService _configurationService;
        private readonly TranscriptionServiceFactory _transcriptionFactory;
        private readonly ILogger _logger;
        private readonly MainWindowViewModel _viewModel;
        
        private ITranscriptionService? _currentTranscriptionService;
        private System.Threading.CancellationTokenSource? _transcriptionCts;
        
        public MainWindow(
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            IConfigurationService configurationService,
            TranscriptionServiceFactory transcriptionFactory,
            ILogger logger,
            MainWindowViewModel viewModel)
        {
            InitializeComponent();
            
            // Injetar dependências
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _transcriptionFactory = transcriptionFactory ?? throw new ArgumentNullException(nameof(transcriptionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            // Configurar DataContext
            this.DataContext = _viewModel;
            
            // Inscrever-se em eventos (em vez de callbacks)
            _eventPublisher.SegmentReceived += OnSegmentReceived;
            _eventPublisher.ErrorOccurred += OnErrorOccurred;
            _eventPublisher.TranscriptionStarted += OnTranscriptionStarted;
            _eventPublisher.TranscriptionCompleted += OnTranscriptionCompleted;
            
            // Inscrever-se em mudanças de config
            _configurationService.ConfigurationChanged += (s, e) => UpdateStatus();
            
            _logger.Info("MainWindow inicializada");
            UpdateStatus();
        }
        
        private async void ButtonIniciar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("=== INICIANDO TRANSCRIÇÃO ===");
                
                // Validar configuração
                if (!_configurationService.IsValid())
                {
                    MessageBox.Show(
                        "Dispositivo não selecionado! Configure em CONFIG primeiro.",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                
                var device = _configurationService.SelectedDevice;
                if (device == null)
                {
                    throw new InvalidOperationException("Dispositivo de áudio não pode ser nulo");
                }
                
                // Limpar histórico anterior
                _viewModel.ClearAllLines();
                _historyManager.Clear();
                
                // Criar serviço de transcrição apropriado
                var option = _configurationService.SelectedOption;
                _currentTranscriptionService = _transcriptionFactory.CreateService(option);
                
                // Iniciar com token de cancelamento
                _transcriptionCts = new System.Threading.CancellationTokenSource();
                buttonIniciar.IsEnabled = false;
                buttonParar.IsEnabled = true;
                
                // Executar transcrição
                await _currentTranscriptionService.StartAsync(device, _transcriptionCts.Token);
                
                _logger.Info("Transcrição concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.Error("Erro ao iniciar transcrição", ex);
                MessageBox.Show(
                    $"Erro: {ex.Message}",
                    "Erro na Transcrição",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                buttonIniciar.IsEnabled = true;
                buttonParar.IsEnabled = false;
            }
        }
        
        private void ButtonParar_Click(object sender, RoutedEventArgs e)
        {
            _logger.Info("Parando transcrição...");
            _currentTranscriptionService?.Stop();
            _transcriptionCts?.Cancel();
        }
        
        // ===== HANDLERS DE EVENTOS =====
        
        private void OnSegmentReceived(object? sender, TranscriptionSegmentReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var segment = e.Segment;
                _logger.Debug($"[UI] Segmento recebido: IsFinal={segment.IsFinal}");
                
                if (segment.IsFinal && !string.IsNullOrWhiteSpace(segment.Text))
                {
                    // Adicionar ao ViewModel
                    string speaker = !string.IsNullOrWhiteSpace(segment.Speaker) 
                        ? segment.Speaker 
                        : "Participante";
                    
                    _viewModel.AddFinalizedLine(segment.Text, speaker);
                    _historyManager.AddMessage(speaker, segment.Text);
                }
                else if (!segment.IsFinal)
                {
                    // Atualizar texto interim
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
                _logger.Error("Erro em transcrição", e.Exception);
                MessageBox.Show(
                    $"Erro: {e.Exception.Message}",
                    "Erro na Transcrição",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                buttonIniciar.IsEnabled = true;
                buttonParar.IsEnabled = false;
            });
        }
        
        private void OnTranscriptionStarted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _logger.Info("Transcrição iniciada");
                statusLabel.Text = "Status: Transcrevendo...";
            });
        }
        
        private void OnTranscriptionCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _logger.Info("Transcrição concluída");
                statusLabel.Text = "Status: Pronto";
                UpdateStatus();
            });
        }
        
        private void UpdateStatus()
        {
            try
            {
                string opcao = _configurationService.SelectedOption;
                string descricaoOpcao = opcao switch
                {
                    "1" => "Transcrição SEM diarização",
                    "2" => "Transcrição COM diarização",
                    "3" => "Apenas capturar áudio",
                    _ => "Nenhuma"
                };
                
                string dispositivo = _configurationService.SelectedDeviceName ?? "Não selecionado";
                statusLabel.Text = $"Modo: {descricaoOpcao} | Dispositivo: {dispositivo}";
            }
            catch (Exception ex)
            {
                _logger.Error("Erro ao atualizar status", ex);
            }
        }
    }
}
```

---

## 5. Configurar Dependency Injection em Program.cs

### Program.cs (Refatorado)
```csharp
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services;
using TraducaoTIME.Services.Events;
using TraducaoTIME.UIWPF;
using TraducaoTIME.UIWPF.ViewModels;
using TraducaoTIME.Utils;
using dotenv.net;

namespace TraducaoTIME
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        
        public new static App Current => (App)Application.Current;
        public IServiceProvider ServiceProvider => _serviceProvider!;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Carregar variáveis de ambiente
                DotEnv.Load();
                
                // Configurar serviços
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();
                
                // Criar janela principal com DI
                var logger = _serviceProvider.GetRequiredService<ILogger>();
                logger.Info("===== APLICAÇÃO INICIADA =====");
                
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro fatal na inicialização:\n{ex.Message}",
                    "Erro Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            // Abstrações Centralizadas
            services.AddSingleton<ILogger, LoggerService>();
            services.AddSingleton<IConfigurationService, AppConfig>();
            services.AddSingleton<IHistoryManager, HistoryManager>();
            services.AddSingleton<ITranscriptionEventPublisher, TranscriptionEventPublisher>();
            
            // Factory
            services.AddSingleton<TranscriptionServiceFactory>();
            
            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            
            // UI
            services.AddSingleton<MainWindow>();
        }
    }
}
```

---

## Resumo das Melhorias

✅ **Interfaces abstratas** eliminam acoplamento
✅ **Event Publisher** substitui callbacks estáticos
✅ **Factory Pattern** permite adicionar novos serviços sem modificar MainWindow
✅ **Dependency Injection** centraliza configuração
✅ **Thin Code-Behind** - MainWindow apenas coordena, não contém lógica
✅ **Logging centralizado** - sem duplicação
✅ **Altamente testável** - fácil mockar dependências

