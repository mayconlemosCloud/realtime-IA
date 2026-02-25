using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using TraducaoTIME.UI;
using TraducaoTIME.Utils;
using TraducaoTIME.Features.TranscricaoSemDiarizacao;
using TraducaoTIME.Features.TranscricaoComDiarizacao;
using TraducaoTIME.Features.CapturaAudio;

class Program
{
    // Importar a função para alocar console
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [STAThread]
    static void Main()
    {
        // Alocar console para debug
        AllocConsole();
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Criar a janela
        MainForm form = new MainForm();

        // Conectar o callback da transcrição à MainForm
        TranscricaoSemDiarizacao.OnTranscriptionReceived = (text) => form.ShowTranslation(text);
        TranscricaoComDiarizacao.OnTranscriptionReceived = (text) => form.ShowTranslation(text);

        // Passar referência de transcrição para MainForm
        form.SetTranscriptionCallbacks(
            (device) => TranscricaoSemDiarizacao.Executar(device),
            (device) => TranscricaoComDiarizacao.Executar(device)
        );

        // Mostrar a janela
        Application.Run(form);
    }
}
