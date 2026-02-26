using System;
using System.IO;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.Logging
{
    /// <summary>
    /// Implementação de saída de log para arquivo.
    /// Escreve simultaneamente em arquivo e console.
    /// </summary>
    public class FileLoggerOutput : ILoggerOutput
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public FileLoggerOutput(string logPath)
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            InitializeFile();
        }

        public void Write(string level, string message)
        {
            lock (_lock)
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.PadRight(7)}] {message}";

                // Escrever no console
                Console.WriteLine(logMessage);

                // Escrever em arquivo
                try
                {
                    File.AppendAllText(_logPath, logMessage + "\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FALHA AO ESCREVER LOG] {ex.Message}");
                }
            }
        }

        private void InitializeFile()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_logPath))
                    {
                        string header = $"========== LOG INICIADO - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\n";
                        File.WriteAllText(_logPath, header);
                    }
                }
                catch
                {
                    // Silenciosamente ignora erros
                }
            }
        }

        public string GetLogPath() => _logPath;
    }
}
