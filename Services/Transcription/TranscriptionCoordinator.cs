using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services;

namespace TraducaoTIME.Services.Transcription
{
    /// <summary>
    /// Coordenador central para gerenciar o ciclo de vida da transcrição.
    /// Simplifica a lógica da UI ao centralizar a coordenação.
    /// </summary>
    public interface ITranscriptionCoordinator
    {
        Task StartTranscriptionAsync(CancellationToken cancellationToken = default);
        void StopTranscription();
        bool IsRunning { get; }
    }

    public class TranscriptionCoordinator : ITranscriptionCoordinator
    {
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly IConfigurationService _configurationService;
        private readonly TranscriptionServiceFactory _factory;
        private readonly ILogger _logger;

        private ITranscriptionService? _currentService;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _currentService != null;

        public TranscriptionCoordinator(
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            IConfigurationService configurationService,
            TranscriptionServiceFactory factory,
            ILogger logger)
        {
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Inicia a transcrição com validações prévias.
        /// </summary>
        public async Task StartTranscriptionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("=== INICIANDO TRANSCRIÇÃO ===");

                // Validar configuração
                if (!_configurationService.IsValid())
                {
                    throw new InvalidOperationException("Dispositivo não configurado.");
                }

                // Limpar histórico
                _historyManager.Clear();

                // Criar serviço apropriado
                var device = _configurationService.SelectedDevice;
                var option = _configurationService.SelectedOption;

                if (device == null)
                {
                    throw new InvalidOperationException("Nenhum dispositivo de áudio selecionado.");
                }

                _currentService = _factory.CreateService(option);
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Executar transcrição
                var result = await _currentService.StartAsync(device, _cts.Token);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorMessage ?? "Erro desconhecido na transcrição");
                }

                _logger.Info("Transcrição concluída com sucesso");
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
                _cts = null;
            }
        }

        /// <summary>
        /// Para a transcrição em progresso.
        /// </summary>
        public void StopTranscription()
        {
            _logger.Info("Parando transcrição...");
            _currentService?.Stop();
            _cts?.Cancel();
        }
    }
}
