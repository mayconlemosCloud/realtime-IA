using System;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace TraducaoTIME.Features.TranscricaoSemDiarizacao
{
    public class TranscricaoSemDiarizacao
    {
        public static void Executar(MMDevice device)
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

                // Configuração do SpeechTranslationConfig
                var config = SpeechTranslationConfig.FromSubscription(azureKey, azureRegion);
                config.SpeechRecognitionLanguage = "en-US"; // Idioma de entrada: Inglês
                config.AddTargetLanguage("pt-BR"); // Idioma de saída para tradução

                Console.WriteLine("✓ Speech Translation (nativo) ativado - Reconhecendo inglês, traduzindo para PT-BR\n");

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
                    // Sem diarização, usa TranslationRecognizer com tradução nativa do Azure Speech
                    using (var translationRecognizer = new TranslationRecognizer(config, audioConfig))
                    {
                        Console.WriteLine("🎤 Iniciando captura e transcrição em tempo real...");
                        Console.WriteLine("Fale agora! Pressione ENTER para parar.\n");
                        Console.WriteLine("═══════════════════════════════════════════\n");

                        capture.StartRecording();
                        bool isFirst = true;

                        translationRecognizer.Recognizing += (s, e) =>
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
                                    Console.WriteLine($"Diarização: NÃO\n");
                                    Console.WriteLine("═══════════════════════════════════════════\n");
                                    isFirst = false;
                                }
                                Console.WriteLine($"[Reconhecendo...] {e.Result.Text}");

                                // Exibe tradução nativa (Speech Translation do Azure)
                                if (e.Result.Translations.ContainsKey("pt-BR"))
                                {
                                    string traducao = e.Result.Translations["pt-BR"];
                                    if (!string.IsNullOrWhiteSpace(traducao))
                                    {
                                        Console.WriteLine($"🌐 {traducao}\n");
                                    }
                                }
                            }
                        };

                        translationRecognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                // Limpa linha parcial
                                Console.Write("\r" + new string(' ', 160) + "\r");

                                // Exibe texto final
                                Console.WriteLine($"👤 [Finalizado] {e.Result.Text}");

                                // Exibe tradução nativa (Speech Translation do Azure)
                                if (e.Result.Translations.ContainsKey("pt-BR"))
                                {
                                    string traducao = e.Result.Translations["pt-BR"];
                                    if (!string.IsNullOrWhiteSpace(traducao))
                                    {
                                        Console.WriteLine($"🌐 [Tradução]  {traducao}\n");
                                    }
                                }
                            }
                        };

                        translationRecognizer.StartContinuousRecognitionAsync().Wait();
                        Console.ReadLine();
                        translationRecognizer.StopContinuousRecognitionAsync().Wait();
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
