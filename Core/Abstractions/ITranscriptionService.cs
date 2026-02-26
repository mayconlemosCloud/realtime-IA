using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace TraducaoTIME.Core.Abstractions
{
    public interface ITranscriptionService
    {
        Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default);
        void Stop();
        string ServiceName { get; }
    }

    public class TranscriptionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalSegments { get; set; }
    }
}
