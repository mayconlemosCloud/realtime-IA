using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services.Logging;

namespace TraducaoTIME.Services.Transcription
{
    public class CapturaAudioService : ITranscriptionService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly ILogger _logger;
        private bool _shouldStop = false;
        private long _totalBytesRecorded = 0;

        public string ServiceName => "Captura de Áudio";

        public CapturaAudioService(
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

        public async Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info($"[{ServiceName}] Iniciando...");
                _eventPublisher.OnTranscriptionStarted();

                IWaveIn capture = device.DataFlow == DataFlow.Render
                    ? new WasapiLoopbackCapture(device)
                    : new WasapiCapture(device);

                capture.WaveFormat = new WaveFormat(16000, 16, 1);

                _totalBytesRecorded = 0;
                _shouldStop = false;

                capture.DataAvailable += (sender, e) =>
                {
                    _totalBytesRecorded += e.BytesRecorded;
                    _logger.Debug($"[{ServiceName}] Capturados {e.BytesRecorded} bytes (Total: {_totalBytesRecorded})");
                };

                var startSegment = new TranscriptionSegment($"🎤 Capturando áudio de {device.FriendlyName}...", isFinal: true);
                _eventPublisher.OnSegmentReceived(startSegment);

                capture.StartRecording();
                _logger.Info($"[{ServiceName}] Captura iniciada");

                while (!_shouldStop && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                capture.StopRecording();
                _logger.Info($"[{ServiceName}] Captura finalizada. Total: {_totalBytesRecorded} bytes");

                var endSegment = new TranscriptionSegment($"✓ Captura finalizada. Total: {_totalBytesRecorded} bytes", isFinal: true);
                _eventPublisher.OnSegmentReceived(endSegment);

                _eventPublisher.OnTranscriptionCompleted();

                return new TranscriptionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.Error($"[{ServiceName}] Erro fatal", ex);
                _eventPublisher.OnErrorOccurred(ex);
                return new TranscriptionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public void Stop()
        {
            _logger.Info($"[{ServiceName}] Parando...");
            _shouldStop = true;
        }
    }
}
