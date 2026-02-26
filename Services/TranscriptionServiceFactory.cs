using System;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services.Transcription;

namespace TraducaoTIME.Services
{
    /// <summary>
    /// Factory para criar serviços de transcrição apropriados baseado na opção selecionada.
    /// Factory Pattern utiliza Strategy Pattern.
    /// </summary>
    public class TranscriptionServiceFactory
    {
        private readonly IConfigurationService _configurationService;
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly ILogger _logger;
        private readonly AppSettings _settings;

        public TranscriptionServiceFactory(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger,
            AppSettings settings)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ITranscriptionService CreateService(string option)
        {
            _logger.Info($"[Factory] Criando serviço para opção: {option}");

            return option switch
            {
                "1" => new TranscricaoSemDiarizacaoService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger,
                    _settings),

                "2" => new TranscricaoComDiarizacaoService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger,
                    _settings),

                "3" => new CapturaAudioService(
                    _configurationService,
                    _eventPublisher,
                    _historyManager,
                    _logger,
                    _settings),

                _ => throw new InvalidOperationException($"Opção de transcrição desconhecida: {option}")
            };
        }
    }
}
