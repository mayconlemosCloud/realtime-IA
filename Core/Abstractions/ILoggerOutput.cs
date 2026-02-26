namespace TraducaoTIME.Core.Abstractions
{
    /// <summary>
    /// Interface para abstração de saída do logger.
    /// Permite múltiplas implementações: File, Console, Cloud, etc.
    /// Strategy Pattern para extensibilidade.
    /// </summary>
    public interface ILoggerOutput
    {
        void Write(string level, string message);
    }
}
