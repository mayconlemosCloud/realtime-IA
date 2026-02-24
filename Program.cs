using System;
using TraducaoTIME.Utils;
using TraducaoTIME.Features.TranscricaoSemDiarizacao;
using TraducaoTIME.Features.TranscricaoComDiarizacao;
using TraducaoTIME.Features.CapturaAudio;

class Program
{
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

        // Seleciona dispositivo
        var device = AudioDeviceSelector.SelecionarDispositivo();

        // Executa a opção selecionada
        if (option == "1")
        {
            TranscricaoSemDiarizacao.Executar(device);
        }
        else if (option == "2")
        {
            TranscricaoComDiarizacao.Executar(device).Wait();
        }
        else
        {
            CapturaAudio.Executar(device);
        }
    }
}
