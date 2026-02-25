using System;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using TraducaoTIME.Utils;

namespace TraducaoTIME.Features.TranscricaoComDiarizacao
{
    public class TranscricaoComDiarizacao
    {
        // Callback para enviar texto para a UI - ambos string e TranscriptionSegment
        public static Action<string>? OnTranscriptionReceivedString { get; set; }
        public static Action<TranscriptionSegment>? OnTranscriptionReceivedSegment { get; set; }

        // Para manter compatibilidade, chamamos o método helper
        public static Action<string>? OnTranscriptionReceived
        {
            get { return OnTranscriptionReceivedString; }
            set { OnTranscriptionReceivedString = value; }
        }

        // Flag para controlar a transcrição
        private static bool _shouldStop = false;

        public static async Task Executar(MMDevice device)
        {
            try
            {
                // Obtém credenciais do Azure
                string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
                string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
                {
                    var errorSegment = new TranscriptionSegment("❌ ERRO: Variáveis de ambiente não configuradas!", isFinal: true);
                    OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                    return;
                }

                // Configuração do Speech Config para diarização
                var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

                var diarizationSegment = new TranscriptionSegment("Diarização ativada", isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(diarizationSegment);

                // Cria captura a partir do dispositivo selecionado
                IWaveIn capture = device.DataFlow == DataFlow.Render
                    ? new WasapiLoopbackCapture(device)
                    : new WasapiCapture(device);

                capture.WaveFormat = new WaveFormat(16000, 16, 1);

                // Cria PushAudioInputStream para streaming
                var pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
                var audioConfig = AudioConfig.FromStreamInput(pushStream);

                // Conecta os eventos do WaveIn ao PushStream
                capture.DataAvailable += (sender, e) =>
                {
                    byte[] buffer = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                    pushStream.Write(buffer);
                };

                using (audioConfig)
                {
                    // Para diarização, usamos ConversationTranscriber
                    using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                    {
                        var startSegment = new TranscriptionSegment("Iniciando captura e transcrição em tempo real COM DIARIZAÇÃO...", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(startSegment);

                        var deviceSegment = new TranscriptionSegment($"Dispositivo: {device.FriendlyName}", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(deviceSegment);

                        var diarSegment = new TranscriptionSegment("Diarização: SIM", isFinal: true, isDiarization: true);
                        OnTranscriptionReceivedSegment?.Invoke(diarSegment);

                        capture.StartRecording();
                        bool isFirst = true;

                        conversationTranscriber.Transcribing += async (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                if (isFirst)
                                {
                                    isFirst = false;
                                }

                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                // Enviar como interim (não final)
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: false, speaker: $"Pessoa {speakerId}", isDiarization: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                                Console.WriteLine($"[Interim] [{speakerId}] {e.Result.Text}");
                            }
                        };

                        conversationTranscriber.Transcribed += async (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Transcribed: {e.Result.Text} | Reason: {e.Result.Reason}");
                            
                            // Aceitar qualquer resultado não vazio como final
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";

                                // Enviar como final
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: true, speaker: $"Pessoa {speakerId}", isDiarization: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                                Console.WriteLine($"[Final] [{speakerId}] {e.Result.Text}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"[DEBUG] NoMatch (silêncio detectado)");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            var cancellation = CancellationDetails.FromResult(e.Result);
                            var errorSegment = new TranscriptionSegment($"❌ ERRO: {cancellation.ErrorDetails}", isFinal: true);
                            OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                            Console.WriteLine($"\n❌ ERRO: {cancellation.ErrorDetails}");
                        };

                        _shouldStop = false;
                        await conversationTranscriber.StartTranscribingAsync();
                        Console.WriteLine("[DEBUG] Transcrição iniciada CSS - aguardando parada");

                        // Aguardar até que a transcrição seja parada
                        while (!_shouldStop)
                        {
                            await Task.Delay(100);
                        }

                        await conversationTranscriber.StopTranscribingAsync();
                        capture.StopRecording();
                    }
                }
            }
            catch (Exception ex)
            {
                string erro = $"❌ ERRO: {ex.Message}";
                var errorSegment = new TranscriptionSegment(erro, isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                OnTranscriptionReceivedString?.Invoke(erro);
                Console.WriteLine(erro);
            }
        }

        public static void Parar()
        {
            _shouldStop = true;
        }
    }
}
