using System;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using TraducaoTIME.Utils;

namespace TraducaoTIME.Features.TranscricaoSemDiarizacao
{
    public class TranscricaoSemDiarizacao
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
                    var errorSegment = new TranscriptionSegment("❌ ERRO: Variáveis de ambiente não configuradas!", isFinal: true);
                    OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                    return;
                }

                Console.WriteLine("[DEBUG] Credenciais encontradas");

                // Configuração do SpeechTranslationConfig
                var config = SpeechTranslationConfig.FromSubscription(azureKey, azureRegion);
                config.SpeechRecognitionLanguage = "en-US"; // Idioma de entrada: Inglês
                config.AddTargetLanguage("pt"); // Idioma de saída para tradução (usar código simples: "pt" não "pt-BR")

                // Testar conexão com Azure ANTES de prosseguir
                Console.WriteLine("[INFO] Testando autenticação com Azure Speech Service...");
                try
                {
                    // Validar fazendo um teste HTTP direto ao serviço Azure
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
                        var testUrl = $"https://{azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

                        try
                        {
                            var response = httpClient.PostAsync(testUrl, new System.Net.Http.StringContent("")).Result;
                            if (!response.IsSuccessStatusCode)
                            {
                                throw new Exception($"Erro {response.StatusCode}: {response.ReasonPhrase}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                                throw new Exception("401: Chave API inválida");
                            if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                                throw new Exception("403: Acesso negado - quota excedida");
                            throw;
                        }
                    }
                    Console.WriteLine("[INFO] ✅ Autenticação Azure validada!\n");
                }
                catch (Exception ex)
                {
                    string errorMsg = ex.Message.ToLower();
                    string merged = (errorMsg).ToLower();

                    string erro = "❌ ERRO DE AUTENTICAÇÃO\n\n";

                    if (merged.Contains("401"))
                    {
                        erro = "❌ ERRO: Chave API inválida!\n\n";
                        erro += "Verifique AZURE_SPEECH_KEY no arquivo .env\n";
                        erro += "A chave pode estar errada, expirada ou não ser válida para esta região.";
                    }
                    else if (merged.Contains("403"))
                    {
                        erro = "❌ ERRO: Sua quota foi excedida!\n\n";
                        erro += "Sua assinatura gratuita pode ter um limite ou expirou.";
                    }
                    else if (merged.Contains("connection") || merged.Contains("timeout") || merged.Contains("network"))
                    {
                        erro = "❌ ERRO: Falha de conexão de rede!\n\n";
                        erro += "Verifique sua conexão com a internet.";
                    }
                    else if (merged.Contains("service unavailable") || merged.Contains("503"))
                    {
                        erro = "❌ ERRO: Serviço Azure indisponível!\n\n";
                        erro += "Tente novamente em alguns minutos.";
                    }
                    else
                    {
                        erro += $"Detalhes: {ex.Message}";
                    }

                    Console.WriteLine($"\n{erro}\n");
                    var errorSegment = new TranscriptionSegment(erro, isFinal: true);
                    OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                    return;
                }

                var infoSegment = new TranscriptionSegment("Speech Translation ativado - Reconhecendo inglês, traduzindo para PT-BR", isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(infoSegment);

                // Cria captura a partir do dispositivo selecionado
                IWaveIn capture = device.DataFlow == DataFlow.Render
                    ? new WasapiLoopbackCapture(device)
                    : new WasapiCapture(device);

                capture.WaveFormat = new WaveFormat(16000, 16, 1);

                // Cria PushAudioInputStream para streaming
                var pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
                var audioConfigForCapture = AudioConfig.FromStreamInput(pushStream);

                // Conecta os eventos do WaveIn ao PushStream
                capture.DataAvailable += (sender, e) =>
                {
                    try
                    {
                        byte[] buffer = new byte[e.BytesRecorded];
                        Array.Copy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                        pushStream.Write(buffer);
                    }
                    catch (Exception exEvent)
                    {
                        Logger.Error($"Erro em DataAvailable: {exEvent.Message}", exEvent);
                    }
                };

                using (audioConfigForCapture)
                {
                    // Sem diarização, usa TranslationRecognizer com tradução nativa do Azure Speech
                    using (var translationRecognizer = new TranslationRecognizer(config, audioConfigForCapture))
                    {
                        Console.WriteLine("[DEBUG] TranslationRecognizer criado");

                        var startSegment = new TranscriptionSegment("Iniciando captura e transcrição em tempo real...", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(startSegment);

                        var deviceSegment = new TranscriptionSegment($"Dispositivo: {device.FriendlyName}", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(deviceSegment);

                        var diarizationSegment = new TranscriptionSegment("Diarização: NÃO", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(diarizationSegment);

                        capture.StartRecording();
                        Console.WriteLine("[DEBUG] Captura iniciada");
                        bool isFirst = true;

                        translationRecognizer.Recognizing += (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Recognizing: {e.Result.Text}");
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                // Obter a tradução em português
                                string displayText = e.Result.Text;
                                if (e.Result.Translations.TryGetValue("pt", out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                                {
                                    displayText = translatedText; // Usar a tradução em português
                                    Console.WriteLine($"[DEBUG] Tradução interim: {displayText}");
                                }
                                else
                                {
                                    Console.WriteLine($"[DEBUG] Interim (sem tradução): {displayText}");
                                }

                                if (isFirst)
                                {
                                    isFirst = false;
                                }
                                // Enviar como interim (não final)
                                var segment = new TranscriptionSegment(displayText, isFinal: false);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                            }
                        };

                        translationRecognizer.Recognized += (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Recognized: {e.Result.Text} | Reason: {e.Result.Reason}");

                            // Obter a tradução em português
                            string displayText = e.Result.Text;
                            if (e.Result.Translations.TryGetValue("pt", out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                            {
                                displayText = translatedText; // Usar a tradução em português
                                Console.WriteLine($"[DEBUG] Texto traduzido: {displayText}");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Sem tradução disponível, usando texto original");
                            }

                            // Aceitar qualquer resultado não vazio como final
                            if (!string.IsNullOrWhiteSpace(displayText))
                            {
                                // Enviar como final
                                var segment = new TranscriptionSegment(displayText, isFinal: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                                Console.WriteLine($"[DEBUG] Final (adicionado): {displayText}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                // Silêncio detectado - sem mensagem
                                Console.WriteLine($"[DEBUG] NoMatch (silêncio detectado)");
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
