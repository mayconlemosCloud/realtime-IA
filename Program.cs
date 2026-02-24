using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;

class Program
{
    static long totalBytesRecorded = 0;
    static byte[] audioBuffer;
    static int bufferPosition = 0;

    static void Main()
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     TRANSCRIÇÃO DE ÁUDIO - AZURE       ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        Console.WriteLine("Escolha uma opção:");
        Console.WriteLine("1 - Transcrição SEM diarização (tempo real)");
        Console.WriteLine("2 - Transcrição COM diarização (tempo real)");
        Console.WriteLine("3 - Apenas capturar áudio (sem transcrição)");
        Console.Write("\nOpção: ");

        string option = Console.ReadLine() ?? "1";

        // Lista dispositivos
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToList();

        Console.WriteLine("\nDispositivos disponíveis:");
        for (int i = 0; i < devices.Count; i++)
            Console.WriteLine($"{i}: {devices[i].FriendlyName} ({devices[i].DataFlow})");

        Console.Write("Escolha o dispositivo: ");
        int deviceIndex = int.Parse(Console.ReadLine() ?? "0");
        var device = devices[deviceIndex];

        Console.WriteLine($"\n✓ Dispositivo selecionado: {device.FriendlyName}\n");

        // Processa transcrição em tempo real se selecionado
        if (option == "1" || option == "2")
        {
            bool useDiarization = option == "2";
            TranscreverAudioEmTempoReal(device, useDiarization).Wait();
        }
        else
        {
            // Apenas captura
            IWaveIn capture = device.DataFlow == DataFlow.Render
                ? new WasapiLoopbackCapture(device)
                : new WasapiCapture(device);

            capture.WaveFormat = new WaveFormat(16000, 16, 1);

            audioBuffer = new byte[16000 * 2 * 30];
            bufferPosition = 0;
            totalBytesRecorded = 0;

            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();

            Console.WriteLine("🎤 Capturando áudio. Pressione ENTER para parar...\n");
            Console.ReadLine();

            capture.StopRecording();
            Console.WriteLine($"\n✓ Captura finalizada. Total: {totalBytesRecorded} bytes");
            capture.Dispose();
        }
    }

    private static void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Copia dados para o buffer
        if (bufferPosition + e.BytesRecorded <= audioBuffer.Length)
        {
            Array.Copy(e.Buffer, 0, audioBuffer, bufferPosition, e.BytesRecorded);
            bufferPosition += e.BytesRecorded;
        }

        totalBytesRecorded += e.BytesRecorded;
        Console.Write(".");
    }

    private static async Task TranscreverAudioEmTempoReal(MMDevice device, bool useDiarization)
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

            // Configuração do Speech Recognizer
            var speechConfig = SpeechConfig.FromSubscription(azureKey, azureRegion);
            speechConfig.SpeechRecognitionLanguage = "pt-BR";
            speechConfig.OutputFormat = OutputFormat.Detailed;

            // Config para diarização
            if (useDiarization)
            {
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
                Console.WriteLine("✓ Diarização ativada\n");
            }

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
                // Para diarização, usamos ConversationTranscriber em vez de SpeechRecognizer
                if (useDiarization)
                {
                    using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                    {
                        Console.WriteLine("🎤 Iniciando captura e transcrição em tempo real COM DIARIZAÇÃO...");
                        Console.WriteLine("Fale agora! Pressione ENTER para parar.\n");
                        Console.WriteLine("═══════════════════════════════════════════\n");

                        capture.StartRecording();
                        bool isFirst = true;

                        conversationTranscriber.Transcribing += (s, e) =>
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
                                    Console.WriteLine($"Diarização: SIM\n");
                                    Console.WriteLine("═══════════════════════════════════════════\n");
                                    isFirst = false;
                                }

                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                Console.WriteLine($"\r[{speakerId}] {e.Result.Text}");
                            }
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                string speakerId = !string.IsNullOrEmpty(e.Result.SpeakerId) ? e.Result.SpeakerId : "Unknown";
                                Console.WriteLine($"\n👤 [{speakerId}] {e.Result.Text}\n");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            var cancellation = CancellationDetails.FromResult(e.Result);
                            Console.WriteLine($"\n❌ ERRO: {cancellation.ErrorDetails}");
                        };

                        await conversationTranscriber.StartTranscribingAsync();

                        Console.ReadLine();

                        await conversationTranscriber.StopTranscribingAsync();
                        capture.StopRecording();
                    }
                }
                else
                {
                    // Sem diarização, usa SpeechRecognizer normal
                    using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                    {
                        Console.WriteLine("🎤 Iniciando captura e transcrição em tempo real...");
                        Console.WriteLine("Fale agora! Pressione ENTER para parar.\n");
                        Console.WriteLine("═══════════════════════════════════════════\n");

                        capture.StartRecording();
                        bool isFirst = true;

                        recognizer.Recognizing += (s, e) =>
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
                                Console.WriteLine($"\r[Reconhecendo...] {e.Result.Text}");
                            }
                        };

                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                            {
                                Console.WriteLine($"\n[Finalizado]     {e.Result.Text}\n");
                            }
                        };

                        recognizer.StartContinuousRecognitionAsync().Wait();
                        Console.ReadLine();
                        recognizer.StopContinuousRecognitionAsync().Wait();
                        capture.StopRecording();
                    }
                }
            }

            Console.WriteLine("\n═══════════════════════════════════════════\n");
            Console.WriteLine("✓ Transcrição finalizada!");

            pushStream.Close();
            capture.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERRO: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    private static string SaveAudioToTempFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "audio_capture.wav");

        try
        {
            using (var writer = new WaveFileWriter(tempFile, new WaveFormat(16000, 1)))
            {
                writer.Write(audioBuffer, 0, bufferPosition);
            }

            if (File.Exists(tempFile))
            {
                var fileInfo = new FileInfo(tempFile);
                Console.WriteLine($"[DEBUG] Arquivo criado: {tempFile} ({fileInfo.Length} bytes)");
            }

            return tempFile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Erro ao salvar áudio: {ex.Message}");
            throw;
        }
    }
}