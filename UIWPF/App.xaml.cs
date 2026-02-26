using System;
using System.Windows;
using System.Threading.Tasks;
using TraducaoTIME.Services.Logging;

namespace TraducaoTIME.UIWPF
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handler para exceções não tratadas em Tasks
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                var ex = args.Exception.InnerException ?? args.Exception;
                Logger.Error($"[App] Exceção não observada em Task: {ex.GetType().Name}: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"[App] Task Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] Stack: {ex.StackTrace}");
                args.SetObserved();
            };

            // Handler para exceções não tratadas na thread principal
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Logger.Error($"[App] Exceção não tratada na AppDomain: {ex?.GetType().Name}: {ex?.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"[App] AppDomain Exception: {ex?.Message}");
                System.Windows.MessageBox.Show($"Erro crítico: {ex?.Message}\n\n{ex?.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Handler para exceções não tratadas na UI
            this.DispatcherUnhandledException += (sender, args) =>
            {
                Logger.Error($"[App] Exceção não tratada no Dispatcher: {args.Exception.GetType().Name}: {args.Exception.Message}", args.Exception);
                System.Diagnostics.Debug.WriteLine($"[App] Dispatcher Exception: {args.Exception.Message}");
                System.Windows.MessageBox.Show($"Erro na interface: {args.Exception.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
