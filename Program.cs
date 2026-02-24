using System;
using System.Linq;
using System.Net.WebSockets;
using System.Net.Http;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Websocket.Client;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    const string ASSEMBLYAI_KEY = "341bfb4ed0c54a68898fae3867b04082"; // Coloque sua API Key aqui
    static WebsocketClient ws;
    static HttpClient httpClient = new HttpClient();

    // Acumula áudio para enviar em chunks de 100ms
    static Queue<byte> audioBuffer = new Queue<byte>();
    const int SAMPLE_RATE = 16000;
    const int BYTES_PER_100MS = SAMPLE_RATE * 2 / 10; // 16000 * 2 bytes (16-bit) / 10 = 3200 bytes

    static async Task Main()
    {
        // Gera token temporário para autenticação
        string token = await GenerateToken(60);
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Erro ao gerar token");
            return;
        }

        // Lista dispositivos
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToList();

        Console.WriteLine("Dispositivos disponíveis:");
        for (int i = 0; i < devices.Count; i++)
            Console.WriteLine($"{i}: {devices[i].FriendlyName} ({devices[i].DataFlow})");

        Console.Write("Escolha o dispositivo: ");
        int deviceIndex = int.Parse(Console.ReadLine() ?? "0");
        var device = devices[deviceIndex];

        // Seleciona captura
        IWaveIn capture = device.DataFlow == DataFlow.Render
            ? new WasapiLoopbackCapture(device)
            : new WasapiCapture(device);

        capture.WaveFormat = new WaveFormat(16000, 1); // PCM 16kHz mono
        capture.DataAvailable += OnDataAvailable;

        // Configura WebSocket AssemblyAI com v3/ws e token
        var url = new Uri($"wss://streaming.assemblyai.com/v3/ws?sample_rate=16000&formatted_finals=true&token={token}");
        var clientWebSocket = new ClientWebSocket();

        ws = new WebsocketClient(url, () => clientWebSocket);
        ws.ReconnectTimeout = TimeSpan.FromSeconds(30);

        ws.MessageReceived.Subscribe(msg =>
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(msg.Text);

                // Processa evento de transcrição (tipo "Turn")
                if (json.TryGetProperty("type", out var type) && type.GetString() == "Turn")
                {
                    if (json.TryGetProperty("transcript", out var transcript))
                    {
                        var text = transcript.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (json.TryGetProperty("end_of_turn", out var eot) && eot.GetBoolean())
                                Console.WriteLine($"\n✓ Turno finalizado: {text}\n");
                            else
                                Console.WriteLine($"\rTranscrevendo: {text}");
                        }
                    }
                }

                if (json.TryGetProperty("error", out var error))
                    Console.WriteLine($"⚠ Erro: {error.GetString()}");
            }
            catch (Exception ex)
            {
                // Suprimir erros de parsing para mensagens não-JSON
            }
        });

        ws.Start();

        capture.StartRecording();
        Console.WriteLine("\n📡 Streaming para AssemblyAI. Pressione ENTER para sair...\n");
        Console.ReadLine();

        capture.StopRecording();
        capture.Dispose();
        ws.Stop(WebSocketCloseStatus.NormalClosure, "Encerrando");
    }

    // Gera token temporário
    static async Task<string> GenerateToken(int expiresInSeconds)
    {
        try
        {
            string url = $"https://streaming.assemblyai.com/v3/token?expires_in_seconds={expiresInSeconds}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", ASSEMBLYAI_KEY);

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(content);
                if (json.TryGetProperty("token", out var tokenProp))
                    return tokenProp.GetString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao gerar token: {ex.Message}");
        }
        return null;
    }

    private static void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Acumula bytes de áudio na fila
        for (int i = 0; i < e.BytesRecorded; i++)
        {
            audioBuffer.Enqueue(e.Buffer[i]);
        }

        // Envia quando acumular 100ms de áudio (3200 bytes)
        while (audioBuffer.Count >= BYTES_PER_100MS)
        {
            byte[] chunk = new byte[BYTES_PER_100MS];
            for (int i = 0; i < BYTES_PER_100MS; i++)
            {
                chunk[i] = audioBuffer.Dequeue();
            }

            if (ws != null)
            {
                try
                {
                    ws.Send(new ArraySegment<byte>(chunk, 0, BYTES_PER_100MS));
                    Console.Write(".");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao enviar áudio: {ex.Message}");
                }
            }
        }
    }
}