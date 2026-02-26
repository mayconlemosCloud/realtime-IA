using System;
using System.IO;
using System.Diagnostics;

namespace TraducaoTIME.Services.Logging
{
    public static class Logger
    {
        private static readonly string _logFilePath;
        private static readonly object _lockObject = new object();

        static Logger()
        {
            // Criar log na pasta do projeto
            string projectFolder = AppDomain.CurrentDomain.BaseDirectory;
            string logFolder = Path.Combine(projectFolder, "Logs");

            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            _logFilePath = Path.Combine(logFolder, $"transacao_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            // Escrever header do log
            WriteToFile($"========== LOG INICIADO - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\n");
            WriteToFile($"Executável: {Process.GetCurrentProcess().MainModule?.FileName}\n");
            WriteToFile($"Versão .NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\n");
            WriteToFile($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\n");
            WriteToFile($"=================================================================\n\n");
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARNING", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            string fullMessage = ex != null
                ? $"{message}\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace:\n{ex.StackTrace}"
                : message;
            Write("ERROR", fullMessage);
        }

        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.PadRight(7)}] {message}";

            // Escrever no console também
            Console.WriteLine(logMessage);

            // Escrever no arquivo
            WriteToFile(logMessage + "\n");
        }

        private static void WriteToFile(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    File.AppendAllText(_logFilePath, message);
                }
                catch
                {
                    // Se falhar em escrever no arquivo, pelo menos mostra no console
                    Console.WriteLine($"[FALHA AO ESCREVER LOG] {message}");
                }
            }
        }

        public static string GetLogFilePath()
        {
            return _logFilePath;
        }

        public static void OpenLogFile()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }
    }
}
