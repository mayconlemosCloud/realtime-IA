using System;

namespace TraducaoTIME.Core.Abstractions
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
    }
}
