namespace TraducaoTIME.Core.Abstractions
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface para serviço de IA.
    /// Abstrai a implementação local ou OpenAI.
    /// </summary>
    public interface IAIService
    {
        string AnalyzeConversationWithRAG(string question, string conversationHistory);
        List<string> ExtractKeywords(string text);
        string GenerateResponse(string question, string context);
    }
}
