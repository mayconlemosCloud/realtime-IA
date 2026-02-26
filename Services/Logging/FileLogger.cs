using System;
using System.IO;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.Logging
{
    /// <summary>
    /// Logger consolidado que escreve em arquivo e console.
    /// Combina Logger + LoggerProvider + FileLoggerOutput em uma única classe.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly string _logLevel;
        private readonly object _lock = new object();

        private const string DEBUG = "DEBUG";
        private const string INFO = "INFO";
        private const string WARNING = "WARNING";
        private const string ERROR = "ERROR";

        public FileLogger(string logPath, string logLevel = "info")
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            _logLevel = logLevel.ToLowerInvariant();
            InitializeFile();
        }

        public void Debug(string message)
        {
            if (ShouldLog(DEBUG))
                LogMessage(DEBUG, message);
        }

        public void Info(string message)
        {
            if (ShouldLog(INFO))
                LogMessage(INFO, message);
        }

        public void Warning(string message)
        {
            if (ShouldLog(WARNING))
                LogMessage(WARNING, message);
        }

        public void Error(string message, Exception? exception = null)
        {
            if (ShouldLog(ERROR))
            {
                string fullMessage = exception != null
                    ? $"{message}\n  Exception: {exception.GetType().Name}: {exception.Message}\n  StackTrace:\n{exception.StackTrace}"
                    : message;
                LogMessage(ERROR, fullMessage);
            }
        }

        private void LogMessage(string level, string message)
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

        private bool ShouldLog(string messageLevel)
        {
            return _logLevel switch
            {
                "debug" => true,
                "info" => messageLevel != DEBUG,
                "warning" => messageLevel is WARNING or ERROR,
                "error" => messageLevel == ERROR,
                _ => true
            };
        }

        private void InitializeFile()
        {
            lock (_lock)
            {
                try
                {
                    string header = $"""
                        ========== LOG INICIADO - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========
                        Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}
                        OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}
                        =================================================================

                        """;
                    File.AppendAllText(_logPath, header);
                }
                catch
                {
                    // Se falhar ao escrever header, tudo bem - pelo menos logs posteriores funcionarão
                }
            }
        }

        public string GetLogFilePath() => _logPath;

        public void OpenLogFile()
        {
            try
            {
                if (File.Exists(_logPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _logPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Error($"Erro ao abrir arquivo de log: {ex.Message}");
            }
        }
    }
}
