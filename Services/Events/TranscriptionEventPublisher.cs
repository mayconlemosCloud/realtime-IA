using System;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Services.Events
{
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
