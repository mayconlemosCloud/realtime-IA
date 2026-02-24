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
        public static async Task Executar(MMDevice device)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   TRANSCRIÇÃO EM TEMPO REAL - AZURE    ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            try
            {
                // Obtém credenciais do Azure
                string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
                string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
                {
                    Console.WriteLine("❌ ERRO: Variáveis de ambiente não configuradas!\n");
                    return;
                }

                // Configuração do Speech Config para diarização
                var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

                Console.WriteLine("✓ Diarização ativada\n");

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
                        Console.WriteLine("🎤 Iniciando captura e transcrição em tempo real COM DIARIZAÇÃO...");
                        Console.WriteLine("Fale agora! Pressione ENTER para parar.\n");
                        Console.WriteLine("═══════════════════════════════════════════\n");

                        capture.StartRecording();
                        bool isFirst = true;

                        conversationTranscriber.Transcribing += async (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                if (isFirst)
                                {
                                    Console.Clear();
                                    Console.WriteLine("╔════════════════════════════════════════╗");
                                    Console.WriteLine("║   TRANSCRIÇÃO EM TEMPO REAL - AZURE    ║");
                                    Console.WriteLine("╚════════════════════════════════════════╝\n");
                                    Console.WriteLine($"Dispositivo: {device.FriendlyName}");
                                    Console.WriteLine($"Diarização: SIM\n");
                                    Console.WriteLine("═══════════════════════════════════════════\n");
                                    isFirst = false;
                                }

                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                Console.WriteLine($"[{speakerId}] {e.Result.Text}");

                                // Traduz em tempo real também
                                try
                                {
                                    string textoTraduzido = await TranslatorService.TraduirTexto(e.Result.Text);
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

                                // Limpa linha parcial anterior
                                Console.Write("\r" + new string(' ', 160) + "\r");

                                // Exibe texto final
                                Console.WriteLine($"👤 [{speakerId}] {e.Result.Text}");

                                // Traduz para PT-BR
                                try
                                {
                                    string textoTraduzido = await TranslatorService.TraduirTexto(e.Result.Text);
                                    Console.WriteLine($"🌐 [{speakerId}] {textoTraduzido}\n");
                                }
                                catch
                                {
                                    Console.WriteLine($"⚠️  Erro na tradução\n");
                                }
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            var cancellation = CancellationDetails.FromResult(e.Result);
                            Console.WriteLine($"\n❌ ERRO: {cancellation.ErrorDetails}");
                        };

                        await conversationTranscriber.StartTranscribingAsync();
                        Console.ReadLine();
                        await conversationTranscriber.StopTranscribingAsync();
                        capture.StopRecording();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }
    }
}
