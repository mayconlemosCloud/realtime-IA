using System;
using System.Collections.Generic;
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
        
        // Rastreamento de falantes para evitar confusão
        private static Dictionary<string, string> _speakerIdMap = new Dictionary<string, string>();
        private static int _speakerCount = 0;
        private static string _lastSpokerId = "";

        public static async Task Executar(MMDevice device)
        {
            try
            {
                // Obtém credenciais do Azure
                string azureKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
                string azureRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(azureKey) || string.IsNullOrWhiteSpace(azureRegion))
                {
                    var errorSegment = new TranscriptionSegment("❌ ERRO: Variáveis de ambiente não configuradas!", isFinal: true);
                    OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                    return;
                }

                // Configuração do Speech Config para diarização
                var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
                speechConfig.SpeechRecognitionLanguage = "pt-BR";
                speechConfig.OutputFormat = OutputFormat.Detailed;
                
                // Otimizações para melhor diarização
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
                
                // Habilitar detalhes adicionais para melhor identificação de falante
                speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500"); // Detecta pausas de 500ms
                
                // Log das configurações
                var configSegment = new TranscriptionSegment("⚙️ Otimizações: Diarização + Segmentação habilitada", isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(configSegment);

                var diarizationSegment = new TranscriptionSegment("Diarização ativada", isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(diarizationSegment);

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
                    // Resetar mapa de falantes para nova sessão
                    _speakerIdMap.Clear();
                    _speakerCount = 0;
                    _lastSpokerId = "";
                    
                    // Para diarização, usamos ConversationTranscriber
                    using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                    {
                        var startSegment = new TranscriptionSegment("🎤 Iniciando captura COM DIARIZAÇÃO OTIMIZADA...", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(startSegment);

                        var deviceSegment = new TranscriptionSegment($"📱 Dispositivo: {device.FriendlyName}", isFinal: true);
                        OnTranscriptionReceivedSegment?.Invoke(deviceSegment);

                        var diarSegment = new TranscriptionSegment("✅ Diarização: SIM | Segmentação: ativada", isFinal: true, isDiarization: true);
                        OnTranscriptionReceivedSegment?.Invoke(diarSegment);

                        capture.StartRecording();
                        bool isFirst = true;

                        conversationTranscriber.Transcribing += async (s, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                if (isFirst)
                                {
                                    isFirst = false;
                                }

                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                string displayName = GetOrMapSpeaker(speakerId);
                                
                                // Log detalhado para debug
                                Console.WriteLine($"[INTERIM] SpeakerId={speakerId} | Mapeado={displayName} | Confiança={e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult)}");
                                Console.WriteLine($"[INTERIM] Texto: {e.Result.Text}");
                                
                                // Enviar como interim (não final)
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: false, speaker: displayName, isDiarization: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                            }
                        };

                        conversationTranscriber.Transcribed += async (s, e) =>
                        {
                            // Aceitar qualquer resultado não vazio como final
                            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                string displayName = GetOrMapSpeaker(speakerId);
                                
                                // Log detalhado com informações de resultado
                                Console.WriteLine($"\n[FINAL] SpeakerId={speakerId} | Mapeado={displayName} | Reason={e.Result.Reason}");
                                Console.WriteLine($"[FINAL] Texto: {e.Result.Text}");
                                Console.WriteLine($"[FINAL] Falante anterior: {_lastSpokerId}");
                                
                                // Atualizar último falante para rastreamento
                                _lastSpokerId = speakerId;

                                // Enviar como final
                                var segment = new TranscriptionSegment(e.Result.Text, isFinal: true, speaker: displayName, isDiarization: true);
                                OnTranscriptionReceivedSegment?.Invoke(segment);
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"[INFO] Silêncio ou áudio não reconhecido");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            var cancellation = CancellationDetails.FromResult(e.Result);
                            var errorSegment = new TranscriptionSegment($"❌ ERRO: {cancellation.ErrorDetails}", isFinal: true);
                            OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                            Console.WriteLine($"\n❌ ERRO: {cancellation.ErrorDetails}");
                        };

                        _shouldStop = false;
                        await conversationTranscriber.StartTranscribingAsync();
                        Console.WriteLine("[DEBUG] Transcrição iniciada CSS - aguardando parada");

                        // Aguardar até que a transcrição seja parada
                        while (!_shouldStop)
                        {
                            await Task.Delay(100);
                        }

                        await conversationTranscriber.StopTranscribingAsync();
                        capture.StopRecording();
                    }
                }
            }
            catch (Exception ex)
            {
                string erro = $"❌ ERRO: {ex.Message}";
                var errorSegment = new TranscriptionSegment(erro, isFinal: true);
                OnTranscriptionReceivedSegment?.Invoke(errorSegment);
                OnTranscriptionReceivedString?.Invoke(erro);
                Console.WriteLine(erro);
            }
        }

        public static void Parar()
        {
            _shouldStop = true;
            _speakerIdMap.Clear();
            _speakerCount = 0;
        }
        
        // Método auxiliar para mapear Speaker IDs do Azure para números consistentes
        private static string GetOrMapSpeaker(string speakerId)
        {
            if (speakerId == "Unknown" || string.IsNullOrEmpty(speakerId))
                return "Pessoa desconhecida";
            
            // Se já mapeamos este ID, retornar o mapeamento existente
            if (_speakerIdMap.ContainsKey(speakerId))
            {
                return _speakerIdMap[speakerId];
            }
            
            // Novo falante! Criar novo mapeamento
            _speakerCount++;
            string displayName = $"Pessoa {_speakerCount}";
            _speakerIdMap[speakerId] = displayName;
            
            Console.WriteLine($"[NOVO FALANTE] Id Azure={speakerId} -> {displayName}");
            return displayName;
        }
    }
}
