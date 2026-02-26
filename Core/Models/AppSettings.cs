namespace TraducaoTIME.Core.Models
{
    /// <summary>
    /// Configurações centralizadas da aplicação.
    /// Carregadas de variáveis de ambiente no Program.cs.
    /// </summary>
    public class AppSettings
    {
        public AzureSettings Azure { get; set; } = new();
        public AISettings AI { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class AzureSettings
    {
        public string SpeechKey { get; set; } = "";
        public string SpeechRegion { get; set; } = "";
    }

    public class AISettings
    {
        public string ApiKey { get; set; } = "";
        public string Provider { get; set; } = "local";
    }

    public class LoggingSettings
    {
        public string Level { get; set; } = "info";
        public string OutputPath { get; set; } = "Logs";
    }
}
