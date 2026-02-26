using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Services.Transcription
{
    /// <summary>
    /// Serviço de simples captura de áudio (sem transcrição ou tradução).
    /// Útil para registrar não processado.
    /// </summary>
    public class CapturaAudioService : BaseTranscriptionService
    {
        private long _totalBytesRecorded = 0;

        public override string ServiceName => "Captura de Áudio";

        public CapturaAudioService(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger,
            AppSettings settings)
            : base(configurationService, eventPublisher, historyManager, logger, settings)
        {
        }

        public override async Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default)
        {
            try
            {
                this.Logger.Info($"[{ServiceName}] Iniciando...");
                EventPublisher.OnTranscriptionStarted();

                IWaveIn capture = CreateWaveCapture(device);

                _totalBytesRecorded = 0;
                ShouldStop = false;

                capture.DataAvailable += (sender, e) =>
                {
                    _totalBytesRecorded += e.BytesRecorded;
                    this.Logger.Debug($"[{ServiceName}] Capturados {e.BytesRecorded} bytes (Total: {_totalBytesRecorded})");
                };

                var startSegment = new TranscriptionSegment($"🎤 Capturando áudio de {device.FriendlyName}...", isFinal: true);
                EventPublisher.OnSegmentReceived(startSegment);

                capture.StartRecording();
                this.Logger.Info($"[{ServiceName}] Captura iniciada");

                while (!ShouldStop && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                capture.StopRecording();
                this.Logger.Info($"[{ServiceName}] Captura finalizada. Total: {_totalBytesRecorded} bytes");

                var endSegment = new TranscriptionSegment($"✓ Captura finalizada. Total: {_totalBytesRecorded} bytes", isFinal: true);
                EventPublisher.OnSegmentReceived(endSegment);

                EventPublisher.OnTranscriptionCompleted();

                return new TranscriptionResult { Success = true };
            }
            catch (Exception ex)
            {
                Logger.Error($"[{ServiceName}] Erro fatal", ex);
                EventPublisher.OnErrorOccurred(ex);
                return new TranscriptionResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
