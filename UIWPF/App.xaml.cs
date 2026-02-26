using System;
using System.Windows;
using System.Threading.Tasks;

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
                Console.WriteLine($"[App] Exceção não observada em Task: {ex.GetType().Name}: {ex.Message}");
                args.SetObserved();
            };

            // Handler para exceções não tratadas na thread principal
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Console.WriteLine($"[App] Exceção não tratada na AppDomain: {ex?.GetType().Name}: {ex?.Message}");
                System.Windows.MessageBox.Show($"Erro crítico: {ex?.Message}\n\n{ex?.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Handler para exceções não tratadas na UI
            this.DispatcherUnhandledException += (sender, args) =>
            {
                Console.WriteLine($"[App] Exceção não tratada no Dispatcher: {args.Exception.GetType().Name}: {args.Exception.Message}");
                System.Windows.MessageBox.Show($"Erro na interface: {args.Exception.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
