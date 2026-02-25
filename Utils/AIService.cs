using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TraducaoTIME.Utils
{
    public class AIService
    {
        private static AIService? _instance;
        private string? _apiKey;
        private string _apiProvider = "local"; // "local" ou "openai"
        private HttpClient _httpClient = new HttpClient();

        public static AIService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AIService();
                }
                return _instance;
            }
        }

        private AIService()
        {
            // Carregar configurações da API se disponível
            LoadAPIConfiguration();
        }

        private void LoadAPIConfiguration()
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("AI_API_KEY");
                var apiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER");

                System.Diagnostics.Debug.WriteLine($"[AIService] API_KEY carregada: {(apiKey != null ? "SIM (tamanho: " + apiKey.Length + ")" : "NÃO")}");
                System.Diagnostics.Debug.WriteLine($"[AIService] AI_PROVIDER: {apiProvider}");

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _apiKey = apiKey;
                    System.Diagnostics.Debug.WriteLine($"[AIService] API Key configurada com sucesso");
                }

                if (!string.IsNullOrWhiteSpace(apiProvider))
                {
                    _apiProvider = apiProvider.ToLower();
                    System.Diagnostics.Debug.WriteLine($"[AIService] Provider setado para: {_apiProvider}");
                }

                System.Diagnostics.Debug.WriteLine($"[AIService] Configuração carregada: Provider={_apiProvider}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] Erro ao carregar configurações: {ex.Message}");
            }
        }

        /// <summary>
        /// Analisa a conversa com base em uma pergunta usando RAG
        /// </summary>
        public string AnalyzeConversationWithRAG(string question, string conversationHistory)
        {
            // Extrair contexto relevante da conversa
            var relevantContext = ExtractRelevantContext(question, conversationHistory);

            // Usar o contexto para gerar uma resposta
            var response = GenerateResponse(question, relevantContext);

            return response;
        }

        /// <summary>
        /// Extrai segmentos relevantes da conversa baseado na pergunta (RAG)
        /// </summary>
        private string ExtractRelevantContext(string question, string conversationHistory)
        {
            if (string.IsNullOrWhiteSpace(conversationHistory))
                return "Sem contexto disponível";

            var keywords = ExtractKeywords(question);
            var relevantLines = new List<string>();

            var lines = conversationHistory.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var lineScore = CalculateRelevanceScore(line, keywords);
                if (lineScore > 0)
                {
                    relevantLines.Add(line);
                }
            }

            // Limitar a 10 linhas mais relevantes
            var topLines = relevantLines.OrderByDescending(l => CalculateRelevanceScore(l, keywords))
                                       .Take(10)
                                       .ToList();

            if (topLines.Count == 0)
                return "Nenhum contexto relevante encontrado";

            return string.Join("\r\n", topLines);
        }

        /// <summary>
        /// Extrai palavras-chave de uma pergunta
        /// </summary>
        private List<string> ExtractKeywords(string question)
        {
            var stopwords = new HashSet<string> { "o", "a", "um", "uma", "de", "para", "com", "é", "foi", "são", "e", "ou", "isso", "este", "esse", "aquele" };

            var keywords = question.ToLower()
                                   .Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(word => word.Length > 2 && !stopwords.Contains(word))
                                   .Distinct()
                                   .ToList();

            return keywords;
        }

        /// <summary>
        /// Calcula score de relevância de uma linha em relação às palavras-chave
        /// </summary>
        private double CalculateRelevanceScore(string line, List<string> keywords)
        {
            double score = 0;
            var lineLower = line.ToLower();

            foreach (var keyword in keywords)
            {
                if (lineLower.Contains(keyword))
                {
                    score += 1.0;
                }
            }

            return score;
        }

        /// <summary>
        /// Gera uma resposta baseada na pergunta e contexto
        /// </summary>
        private string GenerateResponse(string question, string context)
        {
            // Tentar usar OpenAI se configurado
            if (_apiProvider == "openai")
            {
                return CallOpenAI(question, context);
            }

            // Caso contrário, usar análise local
            return GenerateLocalResponse(question, context);
        }

        /// <summary>
        /// Chama OpenAI (ChatGPT) para gerar respostas
        /// </summary>
        private string CallOpenAI(string question, string context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("[OpenAI] INICIANDO CHAMADA OPENAI");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] API Provider: {_apiProvider}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] API Key presente: {!string.IsNullOrWhiteSpace(_apiKey)}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Pergunta: {question}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("[OpenAI] ❌ ERRO: Nenhuma API Key configurada!");
                    System.Diagnostics.Debug.WriteLine("[OpenAI] Verifique o arquivo .env");
                    System.Diagnostics.Debug.WriteLine("[OpenAI] AI_API_KEY=sua-chave-aqui");
                    return GenerateLocalResponse(question, context);
                }

                System.Diagnostics.Debug.WriteLine($"[OpenAI] ✅ API Key encontrada (tamanho: {_apiKey.Length})");

                // Construir prompt
                var systemPrompt = @"Você é um assistente inteligente analisando conversas transcritas. 
Responda de forma clara, concisa e útil baseado no contexto fornecido.";

                var userPrompt = $@"CONTEXTO:
{context}

PERGUNTA:
{question}

Responda baseado no contexto.";

                System.Diagnostics.Debug.WriteLine("[OpenAI] Preparando requisição HTTP...");

                // Chamada HTTP assíncrona
                var result = CallOpenAIAsync(systemPrompt, userPrompt).ConfigureAwait(false).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(result))
                {
                    System.Diagnostics.Debug.WriteLine("[OpenAI] ✅ SUCESSO! Retornando resposta");
                    var response = new StringBuilder();

                    response.AppendLine($"{result}");
                    return response.ToString();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[OpenAI] ⚠️ Resposta NULL, usando análise local");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenAI] ❌ EXCEÇÃO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Stack: {ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine("[OpenAI] ⚠️ Caindo para análise LOCAL");
            return GenerateLocalResponse(question, context);
        }

        /// <summary>
        /// Método assíncrono para chamar OpenAI
        /// </summary>
        private async Task<string> CallOpenAIAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                client.Timeout = TimeSpan.FromSeconds(30);

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Payload: {json.Substring(0, Math.Min(100, json.Length))}...");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine("[OpenAI] Enviando para: https://api.openai.com/v1/chat/completions");

                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content).ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"[OpenAI] Status Code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"[OpenAI] ✅ Resposta recebida!");
                    System.Diagnostics.Debug.WriteLine($"[OpenAI] Tamanho: {responseContent.Length} bytes");

                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message))
                        {
                            if (message.TryGetProperty("content", out var contentProp))
                            {
                                var result = contentProp.GetString() ?? "Sem resposta";
                                System.Diagnostics.Debug.WriteLine($"[OpenAI] Resposta: {result.Substring(0, Math.Min(50, result.Length))}...");
                                return result;
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("[OpenAI] ⚠️ Resposta não tinha formato esperado");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"[OpenAI] ❌ ERRO HTTP: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[OpenAI] Resposta erro: {errorContent}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[OpenAI] 🔑 PROBLEMA: API Key inválida ou expirada!");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenAI] ❌ ERRO HTTP: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Verificar conexão de internet");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenAI] ❌ ERRO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Stack: {ex.StackTrace}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Gera resposta local sem API externa
        /// </summary>
        private string GenerateLocalResponse(string question, string context)
        {
            var response = new StringBuilder();

            response.AppendLine("📚 Análise Local da Conversa");
            response.AppendLine();
            response.AppendLine($"Pergunta: {question}");
            response.AppendLine();
            response.AppendLine("Contexto Relevante:");
            response.AppendLine("─────────────────────────");

            if (context == "Sem contexto disponível" || context == "Nenhum contexto relevante encontrado")
            {
                response.AppendLine(context);
            }
            else
            {
                response.AppendLine(context);
            }

            response.AppendLine();
            response.AppendLine("Análise:");
            response.AppendLine("─────────────────────────");

            // Análise baseada em tipo de pergunta
            response.Append(PerformAnalysis(question, context));

            return response.ToString();
        }

        /// <summary>
        /// Realiza análise da pergunta e contexto
        /// </summary>
        private string PerformAnalysis(string question, string context)
        {
            var analysis = new StringBuilder();
            var questionLower = question.ToLower();

            if (questionLower.Contains("resumo") || questionLower.Contains("summary") || questionLower.Contains("síntese"))
            {
                analysis.AppendLine("Este é um pedido de resumo. Os pontos principais da conversa:");
                var lines = context.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Take(5))
                {
                    analysis.AppendLine($"  • {line}");
                }
            }
            else if (questionLower.Contains("tema") || questionLower.Contains("topic") || questionLower.Contains("assunto"))
            {
                analysis.AppendLine("Tópicos identificados na conversa encontram-se no contexto acima.");
            }
            else if (questionLower.Contains("sentimento") || questionLower.Contains("sentiment") || questionLower.Contains("tom"))
            {
                analysis.AppendLine("Análise de sentimento: A conversa apresenta múltiplos tons e sentimentos.");
            }
            else if (questionLower.Contains("participante") || questionLower.Contains("speaker") || questionLower.Contains("quem"))
            {
                analysis.AppendLine("Participantes identificados no histórico de conversa acima.");
            }
            else if (questionLower.Contains("decisão") || questionLower.Contains("decision") || questionLower.Contains("conclusão"))
            {
                analysis.AppendLine("Procure pelos segmentos relevantes no contexto para identificar decisões tomadas.");
            }
            else
            {
                analysis.AppendLine("Análise baseada no contexto fornecido acima.");
                analysis.AppendLine("Para respostas mais inteligentes, configure uma API de IA em seu arquivo .env");
            }

            return analysis.ToString();
        }

        /// <summary>
        /// Retorna estatísticas da conversa
        /// </summary>
        public Dictionary<string, object> GetConversationStatistics(string conversationHistory)
        {
            var stats = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(conversationHistory))
            {
                stats["total_lines"] = 0;
                stats["total_characters"] = 0;
                stats["speakers"] = new List<string>();
                return stats;
            }

            var lines = conversationHistory.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var speakers = new HashSet<string>();

            foreach (var line in lines)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ":" }, StringSplitOptions.None);
                    if (parts.Length > 0)
                    {
                        speakers.Add(parts[0].Trim());
                    }
                }
            }

            stats["total_lines"] = lines.Length;
            stats["total_characters"] = conversationHistory.Length;
            stats["speakers"] = speakers.ToList();
            stats["average_line_length"] = lines.Average(l => l.Length);

            return stats;
        }
    }
}
