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
        // Callback para enviar texto para a UI
        public static Action<string>? OnTranscriptionReceived { get; set; }
        
        // Flag para controlar a transcrição
        private static bool _shouldStop = false;

        public static void Executar(MMDevice device)
        {
            try
            {
                Console.WriteLine("[DEBUG] TranscricaoSemDiarizacao.Executar iniciado");

                // Obtém credenciais do Azure
                string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
                string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
                {
                    Console.WriteLine("[DEBUG] Credenciais não encontradas");
                    OnTranscriptionReceived?.Invoke("❌ ERRO: Variáveis de ambiente não configuradas!");
                    return;
                }

                Console.WriteLine("[DEBUG] Credenciais encontradas");

                // Configuração do SpeechTranslationConfig
                var config = SpeechTranslationConfig.FromSubscription(azureKey, azureRegion);
                config.SpeechRecognitionLanguage = "en-US"; // Idioma de entrada: Inglês
                config.AddTargetLanguage("pt-BR"); // Idioma de saída para tradução

                OnTranscriptionReceived?.Invoke("✓ Speech Translation ativado - Reconhecendo inglês, traduzindo para PT-BR\n");

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
                        Console.WriteLine("[DEBUG] TranslationRecognizer criado");
                        OnTranscriptionReceived?.Invoke("🎤 Iniciando captura e transcrição em tempo real...");
                        OnTranscriptionReceived?.Invoke($"Dispositivo: {device.FriendlyName}");
                        OnTranscriptionReceived?.Invoke("Diarização: NÃO\n");

                        capture.StartRecording();
                        Console.WriteLine("[DEBUG] Captura iniciada");
                        bool isFirst = true;

                        translationRecognizer.Recognizing += (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Recognizing: {e.Result.Text}");
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                if (isFirst)
                                {
                                    isFirst = false;
                                }
                                string texto = $"[Reconhecendo...] {e.Result.Text}";
                                Console.WriteLine($"[DEBUG] Chamando OnTranscriptionReceived com: {texto}");
                                OnTranscriptionReceived?.Invoke(texto);
                                Console.WriteLine(texto);

                                // Exibe tradução nativa (Speech Translation do Azure)
                                if (e.Result.Translations.ContainsKey("pt-BR"))
                                {
                                    string traducao = e.Result.Translations["pt-BR"];
                                    if (!string.IsNullOrWhiteSpace(traducao))
                                    {
                                        string textoTraduzido = $"🌐 {traducao}";
                                        OnTranscriptionReceived?.Invoke(textoTraduzido);
                                        Console.WriteLine(textoTraduzido);
                                    }
                                }
                            }
                        };

                        translationRecognizer.Recognized += (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Recognized: {e.Result.Text}");
                            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                // Exibe texto final
                                string texto = $"👤 [Finalizado] {e.Result.Text}";
                                Console.WriteLine($"[DEBUG] Chamando OnTranscriptionReceived com: {texto}");
                                OnTranscriptionReceived?.Invoke(texto);
                                Console.WriteLine(texto);

                                // Exibe tradução nativa (Speech Translation do Azure)
                                if (e.Result.Translations.ContainsKey("pt-BR"))
                                {
                                    string traducao = e.Result.Translations["pt-BR"];
                                    if (!string.IsNullOrWhiteSpace(traducao))
                                    {
                                        string textoTraduzido = $"🌐 [Tradução]  {traducao}";
                                        OnTranscriptionReceived?.Invoke(textoTraduzido);
                                        Console.WriteLine(textoTraduzido);
                                    }
                                }
                            }
                        };

                        Console.WriteLine("[DEBUG] Event handlers registrados, iniciando reconhecimento contínuo");
                        _shouldStop = false;
                        translationRecognizer.StartContinuousRecognitionAsync().Wait();
                        Console.WriteLine("[DEBUG] Reconhecimento iniciado - aguardando parada");
                        
                        // Aguardar até que a transcrição seja parada
                        while (!_shouldStop)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        
                        Console.WriteLine("[DEBUG] Parando reconhecimento");
                        translationRecognizer.StopContinuousRecognitionAsync().Wait();
                        capture.StopRecording();
                    }
                }
            }
            catch (Exception ex)
            {
                string erro = $"❌ ERRO: {ex.Message}";
                Console.WriteLine($"[DEBUG] Exception: {ex}");
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
