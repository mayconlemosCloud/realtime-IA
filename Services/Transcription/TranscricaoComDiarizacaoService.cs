using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services.Logging;

namespace TraducaoTIME.Services.Transcription
{
    public class TranscricaoComDiarizacaoService : ITranscriptionService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ITranscriptionEventPublisher _eventPublisher;
        private readonly IHistoryManager _historyManager;
        private readonly ILogger _logger;
        private bool _shouldStop = false;
        private Dictionary<string, string> _speakerIdMap = new Dictionary<string, string>();
        private int _speakerCount = 0;

        public string ServiceName => "Transcrição Com Diarização";

        public TranscricaoComDiarizacaoService(
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

                var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;

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

                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
                speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");

                var configSegment = new TranscriptionSegment("⚙️ Otimizações: Diarização + Segmentação habilitada", isFinal: true);
                _eventPublisher.OnSegmentReceived(configSegment);

                var diarizationSegment = new TranscriptionSegment("Diarização: SIM", isFinal: true);
                _eventPublisher.OnSegmentReceived(diarizationSegment);

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
                    using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfigForCapture))
                    {
                        _logger.Info($"[{ServiceName}] ConversationTranscriber criado");

                        _shouldStop = false;

                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speaker = GetSpeakerName(e.Result.SpeakerId ?? "Unknown");
                                _logger.Debug($"[{ServiceName}] Transcribing: {speaker}: {e.Result.Text}");

                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: false, speaker: speaker);
                                _eventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speaker = GetSpeakerName(e.Result.SpeakerId ?? "Unknown");
                                _logger.Debug($"[{ServiceName}] Transcribed: {speaker}: {e.Result.Text}");

                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: true, speaker: speaker);
                                _eventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            if (e.Reason == CancellationReason.Error)
                            {
                                _logger.Error($"[{ServiceName}] Erro na transcrição: {e.ErrorDetails}");
                                _eventPublisher.OnErrorOccurred(new Exception(e.ErrorDetails));
                            }
                        };

                        capture.StartRecording();
                        await conversationTranscriber.StartTranscribingAsync();
                        _logger.Info($"[{ServiceName}] Transcrição iniciada");

                        while (!_shouldStop && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        await conversationTranscriber.StopTranscribingAsync();
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

        private string GetSpeakerName(string speakerId)
        {
            if (_speakerIdMap.ContainsKey(speakerId))
            {
                return _speakerIdMap[speakerId];
            }

            _speakerCount++;
            string speakerName = $"Falante {_speakerCount}";
            _speakerIdMap[speakerId] = speakerName;
            return speakerName;
        }
    }
}
