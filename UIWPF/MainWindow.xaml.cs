using System;
using System.Windows;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services;
using TraducaoTIME.Services.AI;
using TraducaoTIME.UIWPF.Models;
using TraducaoTIME.UIWPF.ViewModels;

namespace TraducaoTIME.UIWPF
{
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

            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _transcriptionFactory = transcriptionFactory ?? throw new ArgumentNullException(nameof(transcriptionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            this.DataContext = _viewModel;

            // Inscrever-se em eventos
            _eventPublisher.SegmentReceived += OnSegmentReceived;
            _eventPublisher.ErrorOccurred += OnErrorOccurred;
            _eventPublisher.TranscriptionStarted += OnTranscriptionStarted;
            _eventPublisher.TranscriptionCompleted += OnTranscriptionCompleted;

            _configurationService.ConfigurationChanged += (s, e) => UpdateStatus();

            _logger.Info("MainWindow inicializada");
            UpdateStatus();
        }

        private async void ButtonIniciar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("=== INICIANDO TRANSCRIÇÃO ===");

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

                _viewModel.ClearAllLines();
                _historyManager.Clear();

                var option = _configurationService.SelectedOption;
                _currentTranscriptionService = _transcriptionFactory.CreateService(option);

                _transcriptionCts = new System.Threading.CancellationTokenSource();
                buttonIniciar.IsEnabled = false;
                buttonParar.IsEnabled = true;

                var result = await _currentTranscriptionService.StartAsync(device, _transcriptionCts.Token);

                if (!result.Success)
                {
                    _logger.Error($"Transcrição falhou: {result.ErrorMessage}");
                    MessageBox.Show($"Erro: {result.ErrorMessage}", "Erro na Transcrição", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    _logger.Info("Transcrição concluída com sucesso");
                }
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

        private void OnSegmentReceived(object? sender, TranscriptionSegmentReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var segment = e.Segment;
                _logger.Debug($"[UI] Segmento recebido: IsFinal={segment.IsFinal}");

                if (segment.IsFinal && !string.IsNullOrWhiteSpace(segment.Text))
                {
                    string speaker = !string.IsNullOrWhiteSpace(segment.Speaker)
                        ? segment.Speaker
                        : "Participante";

                    _viewModel.AddFinalizedLine(segment.Text, speaker);
                    _historyManager.AddMessage(speaker, segment.Text);
                }
                else if (!segment.IsFinal)
                {
                    _viewModel.CurrentInterimText =
                        !string.IsNullOrWhiteSpace(segment.Speaker)
                        ? $"{segment.Speaker}: {segment.Text}"
                        : segment.Text;
                }

                UpdateDisplay();
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

        private void UpdateDisplay()
        {
            var transcriptionDoc = new System.Windows.Documents.FlowDocument();
            var transcriptionParagraph = new System.Windows.Documents.Paragraph();

            if (string.IsNullOrEmpty(_viewModel.CurrentInterimText))
            {
                transcriptionParagraph.Inlines.Add(new System.Windows.Documents.Run("Aguardando transcrição..."));
            }
            else
            {
                var arrow = new System.Windows.Documents.Run("⟳ ")
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                    FontWeight = System.Windows.FontWeights.Bold,
                    FontSize = 12
                };
                transcriptionParagraph.Inlines.Add(arrow);

                var text = new System.Windows.Documents.Run(_viewModel.CurrentInterimText)
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)),
                    FontStyle = System.Windows.FontStyles.Italic
                };
                transcriptionParagraph.Inlines.Add(text);
            }

            transcriptionDoc.Blocks.Add(transcriptionParagraph);
            transcriptionTextBox.Document = transcriptionDoc;
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

        private void ConfigMenu_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new ConfigWindow(_configurationService, _logger);
            configWindow.Owner = this;
            configWindow.ShowDialog();
            UpdateStatus();
        }

        private void GenerateSuggestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is FinalizedLineItem item)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(async (_) =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.ShowSuggestion = true;
                            item.IsLoadingSuggestion = true;
                        });

                        string conversationContext = _historyManager.GetFormattedHistory();
                        var aiService = AIService.Instance;
                        string suggestion = await aiService.GetEnglishSuggestionWithRAGAsync(item.Text, conversationContext);

                        Dispatcher.Invoke(() =>
                        {
                            item.EnglishSuggestion = suggestion;
                            item.IsLoadingSuggestion = false;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.EnglishSuggestion = $"Erro ao gerar sugestão: {ex.Message}";
                            item.IsLoadingSuggestion = false;
                        });
                    }
                });
            }
        }
    }
}
