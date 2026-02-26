using System;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.Logging
{
    /// <summary>
    /// Implementação de ILogger que utiliza ILoggerOutput.
    /// Centraliza toda a lógica de logging com flexibilidade de saída.
    /// </summary>
    public class LoggerProvider : ILogger
    {
        private readonly ILoggerOutput _output;
        private readonly string _logLevel;

        private const string DEBUG = "DEBUG";
        private const string INFO = "INFO";
        private const string WARNING = "WARNING";
        private const string ERROR = "ERROR";

        public LoggerProvider(ILoggerOutput output, string logLevel = "info")
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _logLevel = logLevel.ToLowerInvariant();
        }

        public void Debug(string message)
        {
            if (ShouldLog(DEBUG))
                _output.Write(DEBUG, message);
        }

        public void Info(string message)
        {
            if (ShouldLog(INFO))
                _output.Write(INFO, message);
        }

        public void Warning(string message)
        {
            if (ShouldLog(WARNING))
                _output.Write(WARNING, message);
        }

        public void Error(string message, Exception? ex = null)
        {
            if (ShouldLog(ERROR))
            {
                string fullMessage = ex != null
                    ? $"{message}\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace:\n{ex.StackTrace}"
                    : message;
                _output.Write(ERROR, fullMessage);
            }
        }

        /// <summary>
        /// Determina se uma mensagem deve ser logada baseado no nível configurado.
        /// </summary>
        private bool ShouldLog(string messageLevel)
        {
            return _logLevel switch
            {
                "debug" => true,
                "info" => messageLevel != "DEBUG",
                "warning" => messageLevel is "WARNING" or "ERROR",
                "error" => messageLevel == "ERROR",
                _ => true
            };
        }
    }
}
