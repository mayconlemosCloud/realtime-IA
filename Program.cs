using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using dotenv.net;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services;
using TraducaoTIME.Services.Configuration;
using TraducaoTIME.Services.Events;
using TraducaoTIME.Services.History;
using TraducaoTIME.Services.Logging;
using TraducaoTIME.UIWPF;
using TraducaoTIME.UIWPF.ViewModels;

namespace TraducaoTIME
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                AllocConsole();

                // Carregar variáveis de ambiente
                DotEnv.Load();

                // Configurar serviços com DI
                var services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();

                var logger = serviceProvider.GetRequiredService<ILogger>();
                logger.Info("===== APLICAÇÃO INICIADA =====");

                // Criar aplicação WPF
                App app = new App();

                // Criar janela principal com DI
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();

                logger.Info("Executando aplicação...");
                app.Run(mainWindow);

                logger.Info("Aplicação finalizada com sucesso");
            }
            catch (Exception ex)
            {
                Logger.Error("ERRO FATAL NA APLICAÇÃO", ex);
                MessageBox.Show(
                    $"Erro fatal:\n{ex.GetType().Name}: {ex.Message}\n\nVerifique o arquivo de log para mais detalhes.",
                    "Erro Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Abstrações Centralizadas
            services.AddSingleton<ILogger, LoggerProvider>();
            services.AddSingleton<IConfigurationService, TraducaoTIME.Services.Configuration.AppConfig>();
            services.AddSingleton<IHistoryManager, TraducaoTIME.Services.History.HistoryManager>();
            services.AddSingleton<ITranscriptionEventPublisher, TranscriptionEventPublisher>();

            // Factory
            services.AddSingleton<TranscriptionServiceFactory>();

            // ViewModels
            services.AddSingleton<MainWindowViewModel>();

            // UI
            services.AddSingleton<MainWindow>();
        }
    }
}
