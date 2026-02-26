using System;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.Logging
{
    /// <summary>
    /// Adapter ILogger para DI que delega para a implementação estática de Logger
    /// </summary>
    public class LoggerProvider : ILogger
    {
        public void Debug(string message) => Logger.Debug(message);

        public void Info(string message) => Logger.Info(message);

        public void Warning(string message) => Logger.Warning(message);

        public void Error(string message, Exception? exception = null) => Logger.Error(message, exception);
    }
}
