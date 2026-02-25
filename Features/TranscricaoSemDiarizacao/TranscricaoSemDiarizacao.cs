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
                config.AddTargetLanguage("pt-BR"); // Idioma de saída para tradução

                var infoSegment = new TranscriptionSegment("Speech Translation ativado - Reconhecendo inglês", isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(infoSegment);

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
                                if (isFirst)
                                {
                                    isFirst = false;
                                }
                                // Enviar como interim (não final)
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: false);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                                Console.WriteLine($"[DEBUG] Interim: {e.Result.Text}");
                            }
                        };

                        translationRecognizer.Recognized += (s, e) =>
                        {
                            Console.WriteLine($"[DEBUG] Recognized: {e.Result.Text} | Reason: {e.Result.Reason}");
                            
                            // Aceitar qualquer resultado não vazio como final
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                // Enviar como final
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                                Console.WriteLine($"[DEBUG] Final (adicionado): {e.Result.Text}");
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
