using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;

namespace TraducaoTIME.Services.Transcription
{
    /// <summary>
    /// Classe base abstrata para serviços de transcrição.
    /// Extrai responsabilidades comuns: validação Azure, captura/áudio, tratamento de erros.
    /// </summary>
    public abstract class BaseTranscriptionService : ITranscriptionService
    {
        protected readonly IConfigurationService ConfigurationService;
        protected readonly ITranscriptionEventPublisher EventPublisher;
        protected readonly IHistoryManager HistoryManager;
        protected readonly ILogger Logger;
        protected readonly AppSettings Settings;

        protected bool ShouldStop = false;

        public abstract string ServiceName { get; }

        protected BaseTranscriptionService(
            IConfigurationService configurationService,
            ITranscriptionEventPublisher eventPublisher,
            IHistoryManager historyManager,
            ILogger logger,
            AppSettings settings)
        {
            ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            HistoryManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Inicia a transcrição de áudio.
        /// Implementações concretas devem sobrescrever este método.
        /// </summary>
        public abstract Task<TranscriptionResult> StartAsync(MMDevice device, CancellationToken cancellationToken = default);

        /// <summary>
        /// Para a transcrição em progresso.
        /// </summary>
        public virtual void Stop()
        {
            this.Logger.Info($"[{ServiceName}] Parando...");
            ShouldStop = true;
        }

        /// <summary>
        /// Valida as credenciais da Azure Speech API.
        /// Reutilizável por todas as implementações.
        /// </summary>
        protected async Task<(bool Success, string ErrorMessage)> ValidateAzureCredentialsAsync()
        {
            string azureKey = Settings.Azure.SpeechKey;
            string azureRegion = Settings.Azure.SpeechRegion;

            if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
            {
                return (false, "❌ ERRO: Variáveis de ambiente não configuradas!");
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureKey);
                    var testUrl = $"https://{azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
                    var response = await httpClient.PostAsync(testUrl, new StringContent(""));

                    if (!response.IsSuccessStatusCode)
                    {
                        string erro = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                            ? "❌ ERRO: Chave API inválida!"
                            : response.StatusCode == System.Net.HttpStatusCode.Forbidden
                            ? "❌ ERRO: Quota foi excedida!"
                            : $"❌ ERRO: {response.StatusCode}";
                        return (false, erro);
                    }
                }
                this.Logger.Info($"[{ServiceName}] Autenticação Azure validada");
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"❌ ERRO DE CONEXÃO: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper para criar captura de áudio (Loopback ou Microphone).
        /// </summary>
        protected IWaveIn CreateWaveCapture(MMDevice device)
        {
            IWaveIn capture = device.DataFlow == DataFlow.Render
                ? new WasapiLoopbackCapture(device)
                : new WasapiCapture(device);

            capture.WaveFormat = new WaveFormat(16000, 16, 1);
            return capture;
        }

        /// <summary>
        /// Helper para criar PushAudioInputStream para Push Stream.
        /// </summary>
        protected Microsoft.CognitiveServices.Speech.Audio.PushAudioInputStream CreatePushStream()
        {
            return Microsoft.CognitiveServices.Speech.Audio.AudioInputStream.CreatePushStream(
                Microsoft.CognitiveServices.Speech.Audio.AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)) as Microsoft.CognitiveServices.Speech.Audio.PushAudioInputStream;
        }
    }
}
