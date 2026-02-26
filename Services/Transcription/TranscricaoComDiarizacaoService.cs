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

namespace TraducaoTIME.Services.Transcription
{
    /// <summary>
    /// Serviço de transcrição com diarização (identificação de falantes).
    /// Reconhece múltiplos falantes em português.
    /// </summary>
    public class TranscricaoComDiarizacaoService : BaseTranscriptionService
    {
        private readonly Dictionary<string, string> _speakerIdMap = new Dictionary<string, string>();
        private int _speakerCount = 0;

        public override string ServiceName => "Transcrição Com Diarização";

        public TranscricaoComDiarizacaoService(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger,
            AppSettings settings)
            : base(configurationService, eventPublisher, historyManager, logger, settings)
        {
        }

        public override async Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default)
        {
            try
            {
                this.Logger.Info($"[{ServiceName}] Iniciando...");
                EventPublisher.OnTranscriptionStarted();

                // Validar credenciais Azure
                var (success, errorMessage) = await ValidateAzureCredentialsAsync();
                if (!success)
                {
                    EventPublisher.OnErrorOccurred(new InvalidOperationException(errorMessage));
                    return new TranscriptionResult { Success = false, ErrorMessage = errorMessage };
                }

                var speechConfig = SpeechConfig.FromSubscription(Settings.Azure.SpeechKey, Settings.Azure.SpeechRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;

                // Testar conexão
                try
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Settings.Azure.SpeechKey);
                        var testUrl = $"https://{Settings.Azure.SpeechRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
                        var response = await httpClient.PostAsync(testUrl, new System.Net.Http.StringContent(""));
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException($"Erro {response.StatusCode}: {response.ReasonPhrase}");
                        }
                    }
                    this.Logger.Info($"[{ServiceName}] Autenticação Azure validada");
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message.ToLower();
                    string erro = errorMsg.Contains("401") ? "❌ ERRO: Chave API inválida!" :
                                  errorMsg.Contains("403") ? "❌ ERRO: Quota foi excedida!" :
                                  errorMsg.Contains("connection") ? "❌ ERRO: Falha de conexão!" :
                                  $"❌ ERRO DE AUTENTICAÇÃO: {ex.Message}";

                    EventPublisher.OnErrorOccurred(new InvalidOperationException(erro));
                    return new TranscriptionResult { Success = false, ErrorMessage = erro };
                }

                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
                speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");

                var configSegment = new TranscriptionSegment("⚙️ Otimizações: Diarização + Segmentação habilitada", isFinal: true);
                EventPublisher.OnSegmentReceived(configSegment);

                var diarizationSegment = new TranscriptionSegment("Diarização: SIM", isFinal: true);
                EventPublisher.OnSegmentReceived(diarizationSegment);

                IWaveIn capture = CreateWaveCapture(device);
                var pushStream = CreatePushStream();
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
                        Logger.Error($"[{ServiceName}] Erro em DataAvailable", ex);
                    }
                };

                using (audioConfigForCapture)
                {
                    using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfigForCapture))
                    {
                        this.Logger.Info($"[{ServiceName}] ConversationTranscriber criado");

                        ShouldStop = false;

                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speaker = GetSpeakerName(e.Result.SpeakerId ?? "Unknown");
                                this.Logger.Debug($"[{ServiceName}] Transcribing: {speaker}: {e.Result.Text}");

                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: false, speaker: speaker);
                                EventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speaker = GetSpeakerName(e.Result.SpeakerId ?? "Unknown");
                                this.Logger.Debug($"[{ServiceName}] Transcribed: {speaker}: {e.Result.Text}");

                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: true, speaker: speaker);
                                EventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            if (e.Reason == CancellationReason.Error)
                            {
                                Logger.Error($"[{ServiceName}] Erro na transcrição: {e.ErrorDetails}");
                                EventPublisher.OnErrorOccurred(new Exception(e.ErrorDetails));
                            }
                        };

                        capture.StartRecording();
                        await conversationTranscriber.StartTranscribingAsync();
                        this.Logger.Info($"[{ServiceName}] Transcrição iniciada");

                        while (!ShouldStop && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        await conversationTranscriber.StopTranscribingAsync();
                        capture.StopRecording();
                    }
                }

                EventPublisher.OnTranscriptionCompleted();
                this.Logger.Info($"[{ServiceName}] Concluído com sucesso");
                return new TranscriptionResult { Success = true };
            }
            catch (Exception ex)
            {
                Logger.Error($"[{ServiceName}] Erro fatal", ex);
                EventPublisher.OnErrorOccurred(ex);
                return new TranscriptionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override void Stop()
        {
            this.Logger.Info($"[{ServiceName}] Parando...");
            ShouldStop = true;
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
