using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TraducaoTIME.Features.CapturaAudio
{
    public class CapturaAudio
    {
        private static long totalBytesRecorded = 0;
        private static byte[]? audioBuffer;
        private static int bufferPosition = 0;

        public static void Executar(MMDevice device)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║      CAPTURA DE ÁUDIO - AZURE          ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            IWaveIn capture = device.DataFlow == DataFlow.Render
                ? new WasapiLoopbackCapture(device)
                : new WasapiCapture(device);

            capture.WaveFormat = new WaveFormat(16000, 16, 1);

            audioBuffer = new byte[16000 * 2 * 30];
            bufferPosition = 0;
            totalBytesRecorded = 0;

            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();

            Console.WriteLine("🎤 Capturando áudio. Pressione ENTER para parar...\n");
            Console.ReadLine();

            capture.StopRecording();
            Console.WriteLine($"\n✓ Captura finalizada. Total: {totalBytesRecorded} bytes");
            capture.Dispose();
        }

        private static void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            // Copia dados para o buffer
            if (audioBuffer != null && bufferPosition + e.BytesRecorded <= audioBuffer.Length)
            {
                Array.Copy(e.Buffer, 0, audioBuffer, bufferPosition, e.BytesRecorded);
                bufferPosition += e.BytesRecorded;
            }

            totalBytesRecorded += e.BytesRecorded;
            Console.Write(".");
        }
    }
}
