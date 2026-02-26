using System;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Core.Abstractions
{
    public interface ITranscriptionEventPublisher
    {
        event EventHandler<TranscriptionSegmentReceivedEventArgs>? SegmentReceived;
        event EventHandler<TranscriptionErrorEventArgs>? ErrorOccurred;
        event EventHandler<EventArgs>? TranscriptionStarted;
        event EventHandler<EventArgs>? TranscriptionCompleted;

        void OnSegmentReceived(TranscriptionSegment segment);
        void OnErrorOccurred(Exception exception, string? context = null);
        void OnTranscriptionStarted();
        void OnTranscriptionCompleted();
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
