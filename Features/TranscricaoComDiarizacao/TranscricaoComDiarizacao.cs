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
        // Callback para enviar texto para a UI
        public static Action<string>? OnTranscriptionReceived { get; set; }
        
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
                    OnTranscriptionReceived?.Invoke("❌ ERRO: Variáveis de ambiente não configuradas!");
                    return;
                }

                // Configuração do Speech Config para diarização
                var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

                OnTranscriptionReceived?.Invoke("✓ Diarização ativada\n");

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
                        OnTranscriptionReceived?.Invoke("🎤 Iniciando captura e transcrição em tempo real COM DIARIZAÇÃO...");
                        OnTranscriptionReceived?.Invoke($"Dispositivo: {device.FriendlyName}");
                        OnTranscriptionReceived?.Invoke("Diarização: SIM\n");

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
                                string texto = $"[{speakerId}] {e.Result.Text}";
                                OnTranscriptionReceived?.Invoke(texto);
                                Console.WriteLine(texto);

                                // Traduz em tempo real também
                                try
                                {
                                    string textoTraduzido = await TranslatorService.TraduirTexto(e.Result.Text);
                                    OnTranscriptionReceived?.Invoke($"🌐 {textoTraduzido}");
                                    Console.WriteLine($"🌐 {textoTraduzido}\n");
                                }
                                catch
                                {
                                    // Silencia erro de tradução parcial
                                }
                            }
                        };

                        conversationTranscriber.Transcribed += async (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";

                                // Exibe texto final
                                string texto = $"👤 [{speakerId}] {e.Result.Text}";
                                OnTranscriptionReceived?.Invoke(texto);
                                Console.WriteLine(texto);

                                // Traduz para PT-BR
                                try
                                {
                                    string textoTraduzido = await TranslatorService.TraduirTexto(e.Result.Text);
                                    string textoComTrad = $"🌐 [{speakerId}] {textoTraduzido}";
                                    OnTranscriptionReceived?.Invoke(textoComTrad);
                                    Console.WriteLine(textoComTrad + "\n");
                                }
                                catch
                                {
                                    OnTranscriptionReceived?.Invoke("⚠️  Erro na tradução");
                                    Console.WriteLine($"⚠️  Erro na tradução\n");
                                }
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            var cancellation = CancellationDetails.FromResult(e.Result);
                            OnTranscriptionReceived?.Invoke($"❌ ERRO: {cancellation.ErrorDetails}");
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
                OnTranscriptionReceived?.Invoke(erro);
                Console.WriteLine(erro);
            }
        }

        public static void Parar()
        {
            _shouldStop = true;
        }
    }
}
