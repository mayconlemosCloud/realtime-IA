using System;
using System.Runtime.InteropServices;
using System.Windows;
using dotenv.net;
using TraducaoTIME.UIWPF;
using TraducaoTIME.Utils;

namespace TraducaoTIME
{
    class Program
    {
        // Importar a função para alocar console
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Alocar console para debug
                AllocConsole();
                
                Logger.Info("===== APLICAÇÃO INICIADA =====");
                
                // Carregar variáveis do arquivo .env
                Logger.Info("Carregando variáveis de ambiente (.env)...");
                DotEnv.Load();
                Logger.Info("Variáveis de ambiente carregadas com sucesso");

                Logger.Info("Criando aplicação WPF...");
                // Criar aplicação WPF
                App app = new App();

                Logger.Info("Criando janela principal...");
                // Criar janela principal
                MainWindow mainWindow = new MainWindow();

                Logger.Info("Executando aplicação...");
                // Executar aplicação
                app.Run(mainWindow);
                
                Logger.Info("Aplicação finalizada com sucesso");
            }
            catch (Exception ex)
            {
                Logger.Error("ERRO FATAL NA APLICAÇÃO", ex);
                System.Windows.MessageBox.Show(
                    $"Erro fatal:\n{ex.GetType().Name}: {ex.Message}\n\nVerifique o arquivo de log para mais detalhes.",
                    "Erro Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
