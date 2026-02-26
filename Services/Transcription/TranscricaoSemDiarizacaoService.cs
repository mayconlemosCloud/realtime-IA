using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services.Logging;

namespace TraducaoTIME.Services.Transcription
{
    public class TranscricaoSemDiarizacaoService : ITranscriptionService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly ILogger _logger;
        private bool _shouldStop = false;

        public string ServiceName => "Transcrição Sem Diarização";

        public TranscricaoSemDiarizacaoService(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info($"[{ServiceName}] Iniciando...");
                _eventPublisher.OnTranscriptionStarted();

                string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
                string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
                {
                    var error = "❌ ERRO: Variáveis de ambiente não configuradas!";
                    _eventPublisher.OnErrorOccurred(new InvalidOperationException(error));
                    return new TranscriptionResult { Success = false, ErrorMessage = error };
                }

                var config = SpeechTranslationConfig.FromSubscription(azureKey, azureRegion);
                config.SpeechRecognitionLanguage = "en-US";
                config.AddTargetLanguage("pt");

                // Testar conexão
                try
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
                        var testUrl = $"https://{azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
                        var response = await httpClient.PostAsync(testUrl, new System.Net.Http.StringContent(""));
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Erro {response.StatusCode}: {response.ReasonPhrase}");
                        }
                    }
                    _logger.Info($"[{ServiceName}] Autenticação Azure validada");
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message.ToLower();
                    string erro = errorMsg.Contains("401") ? "❌ ERRO: Chave API inválida!" :
                                  errorMsg.Contains("403") ? "❌ ERRO: Quota foi excedida!" :
                                  errorMsg.Contains("connection") ? "❌ ERRO: Falha de conexão!" :
                                  $"❌ ERRO DE AUTENTICAÇÃO: {ex.Message}";

                    _eventPublisher.OnErrorOccurred(new InvalidOperationException(erro));
                    return new TranscriptionResult { Success = false, ErrorMessage = erro };
                }

                IWaveIn capture = device.DataFlow == DataFlow.Render
                    ? new WasapiLoopbackCapture(device)
                    : new WasapiCapture(device);

                capture.WaveFormat = new WaveFormat(16000, 16, 1);

                var pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
                var audioConfigForCapture = AudioConfig.FromStreamInput(pushStream);

                capture.DataAvailable += (sender, e) =>
                {
                    try
                    {
                        byte[] buffer = new byte[e.BytesRecorded];
                        Array.Copy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                        pushStream.Write(buffer);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[{ServiceName}] Erro em DataAvailable", ex);
                    }
                };

                using (audioConfigForCapture)
                {
                    using (var translationRecognizer = new TranslationRecognizer(config, audioConfigForCapture))
                    {
                        _logger.Info($"[{ServiceName}] TranslationRecognizer criado");

                        var infoSegment = new TranscriptionSegment("Speech Translation ativado - Reconhecendo inglês, traduzindo para PT-BR", isFinal: true);
                        _eventPublisher.OnSegmentReceived(infoSegment);

                        var deviceSegment = new TranscriptionSegment($"Dispositivo: {device.FriendlyName}", isFinal: true);
                        _eventPublisher.OnSegmentReceived(deviceSegment);

                        var diarizationSegment = new TranscriptionSegment("Diarização: NÃO", isFinal: true);
                        _eventPublisher.OnSegmentReceived(diarizationSegment);

                        _shouldStop = false;
                        bool isFirst = true;

                        translationRecognizer.Recognizing += (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string displayText = e.Result.Text;
                                if (e.Result.Translations.TryGetValue("pt", out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                                {
                                    displayText = translatedText;
                                }

                                if (isFirst)
                                {
                                    isFirst = false;
                                }

                                var segment = new TranscriptionSegment(displayText, isFinal: false);
                                _eventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        translationRecognizer.Recognized += (s, e) =>
                        {
                            string displayText = e.Result.Text;
                            if (e.Result.Translations.TryGetValue("pt", out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                            {
                                displayText = translatedText;
                            }

                            if (!string.IsNullOrWhiteSpace(displayText))
                            {
                                var segment = new TranscriptionSegment(displayText, isFinal: true);
                                _eventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        capture.StartRecording();
                        await translationRecognizer.StartContinuousRecognitionAsync();
                        _logger.Info($"[{ServiceName}] Reconhecimento iniciado");

                        while (!_shouldStop && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        await translationRecognizer.StopContinuousRecognitionAsync();
                        capture.StopRecording();
                    }
                }

                _eventPublisher.OnTranscriptionCompleted();
                _logger.Info($"[{ServiceName}] Concluído com sucesso");
                return new TranscriptionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.Error($"[{ServiceName}] Erro fatal", ex);
                _eventPublisher.OnErrorOccurred(ex);
                return new TranscriptionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public void Stop()
        {
            _logger.Info($"[{ServiceName}] Parando...");
            _shouldStop = true;
        }
    }
}
