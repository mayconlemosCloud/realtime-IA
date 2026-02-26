using System;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services.Transcription;

namespace TraducaoTIME.Services
{
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
    }
}
