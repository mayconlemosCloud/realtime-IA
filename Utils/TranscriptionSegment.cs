using System;

namespace TraducaoTIME.Utils
{
    public class TranscriptionSegment
    {
        public string Text { get; set; } = "";
        public bool IsFinal { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Speaker { get; set; } = null;
        public bool IsDiarization { get; set; } = false;

        public TranscriptionSegment(string text, bool isFinal = true, string? speaker = null, bool isDiarization = false)
        {
            Text = text;
            IsFinal = isFinal;
            Speaker = speaker;
            IsDiarization = isDiarization;
        }
    }
}
