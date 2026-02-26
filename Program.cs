using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using dotenv.net;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Core.Models;
using TraducaoTIME.Services;
using TraducaoTIME.Services.AI;
using TraducaoTIME.Services.Configuration;
using TraducaoTIME.Services.Events;
using TraducaoTIME.Services.History;
using TraducaoTIME.Services.Logging;
using TraducaoTIME.Services.Transcription;
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
                Console.WriteLine($"[ERRO FATAL] {ex.GetType().Name}: {ex.Message}");
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
            // Configurações centralizadas (AppSettings)
            var appSettings = new AppSettings
            {
                Azure = new AzureSettings
                {
                    SpeechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
                    SpeechRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? ""
                },
                AI = new AISettings
                {
                    ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? "",
                    Provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "local"
                },
                Logging = new LoggingSettings
                {
                    Level = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "info",
                    OutputPath = Environment.GetEnvironmentVariable("LOG_PATH") ?? "Logs"
                }
            };
            services.AddSingleton(appSettings);

            // Logging com suporte a múltiplas saídas
            string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettings.Logging.OutputPath);
            Directory.CreateDirectory(logFolder);
            string logPath = Path.Combine(logFolder, $"transacao_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            services.AddSingleton<ILoggerOutput>(sp => new FileLoggerOutput(logPath));
            services.AddSingleton<ILogger>(sp =>
                new LoggerProvider(sp.GetRequiredService<ILoggerOutput>(), appSettings.Logging.Level));

            // Histórico - com separação de responsabilidades (In-Memory + Storage)
            string historyFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TraducaoTIME", "Historico");
            Directory.CreateDirectory(historyFolder);
            string historyPath = Path.Combine(historyFolder, $"conversa_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            services.AddSingleton<IHistoryStorage>(sp => new FileHistoryStorage(historyPath));
            services.AddSingleton<IHistoryManager>(sp =>
                new HistoryManager(sp.GetRequiredService<IHistoryStorage>(), sp.GetRequiredService<ILogger>()));

            // Configuração e Áudio
            services.AddSingleton<IConfigurationService, AppConfig>();

            // Eventos
            services.AddSingleton<ITranscriptionEventPublisher>(sp =>
                new TranscriptionEventPublisher(sp.GetRequiredService<ILogger>()));

            // IA - Serviço com HttpClient
            services.AddHttpClient<IAIService, AIService>();

            // Factory e Coordenador
            services.AddSingleton<TranscriptionServiceFactory>();
            services.AddSingleton<ITranscriptionCoordinator, TranscriptionCoordinator>();

            // ViewModels
            services.AddSingleton<MainWindowViewModel>();

            // UI
            services.AddSingleton<MainWindow>();
        }
    }
}
