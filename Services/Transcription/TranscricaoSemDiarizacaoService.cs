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

namespace TraducaoTIME.Services.Transcription
{
    /// <summary>
    /// Serviço de transcrição sem diarização.
    /// Reconhece inglês e traduz para português em tempo real.
    /// </summary>
    public class TranscricaoSemDiarizacaoService : BaseTranscriptionService
    {
        public override string ServiceName => "Transcrição Sem Diarização";

        public TranscricaoSemDiarizacaoService(
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
                Logger.Info($"[{ServiceName}] Iniciando...");
                EventPublisher.OnTranscriptionStarted();

                // Validar credenciais Azure
                var (success, errorMessage) = await ValidateAzureCredentialsAsync();
                if (!success)
                {
                    EventPublisher.OnErrorOccurred(new InvalidOperationException(errorMessage));
                    return new TranscriptionResult { Success = false, ErrorMessage = errorMessage };
                }

                var config = SpeechTranslationConfig.FromSubscription(Settings.Azure.SpeechKey, Settings.Azure.SpeechRegion);
                config.SpeechRecognitionLanguage = "en-US";
                config.AddTargetLanguage("pt");

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
                    using (var translationRecognizer = new TranslationRecognizer(config, audioConfigForCapture))
                    {
                        Logger.Info($"[{ServiceName}] TranslationRecognizer criado");

                        var infoSegment = new TranscriptionSegment("Speech Translation ativado - Reconhecendo inglês, traduzindo para PT-BR", isFinal: true);
                        EventPublisher.OnSegmentReceived(infoSegment);

                        var deviceSegment = new TranscriptionSegment($"Dispositivo: {device.FriendlyName}", isFinal: true);
                        EventPublisher.OnSegmentReceived(deviceSegment);

                        var diarizationSegment = new TranscriptionSegment("Diarização: NÃO", isFinal: true);
                        EventPublisher.OnSegmentReceived(diarizationSegment);

                        ShouldStop = false;
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
                                EventPublisher.OnSegmentReceived(segment);
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
                                EventPublisher.OnSegmentReceived(segment);
                            }
                        };

                        capture.StartRecording();
                        await translationRecognizer.StartContinuousRecognitionAsync();
                        Logger.Info($"[{ServiceName}] Reconhecimento iniciado");

                        while (!ShouldStop && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        await translationRecognizer.StopContinuousRecognitionAsync();
                        capture.StopRecording();
                    }
                }

                EventPublisher.OnTranscriptionCompleted();
                Logger.Info($"[{ServiceName}] Concluído com sucesso");
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
            Logger.Info($"[{ServiceName}] Parando...");
            ShouldStop = true;
        }
    }
}
