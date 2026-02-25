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
        /// Quebra linhas longas em múltiplas linhas para melhor legibilidade
        /// </summary>
        private string BreakLongLines(string text, int maxLength = 90)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Length <= maxLength)
                {
                    result.AppendLine(line);
                }
                else
                {
                    // Quebrar linhas longas em palavras
                    var words = line.Split(' ');
                    var currentLine = new StringBuilder();

                    foreach (var word in words)
                    {
                        if ((currentLine.Length + word.Length + 1) > maxLength && currentLine.Length > 0)
                        {
                            result.AppendLine(currentLine.ToString());
                            currentLine = new StringBuilder(word);
                        }
                        else
                        {
                            if (currentLine.Length > 0)
                                currentLine.Append(" ");
                            currentLine.Append(word);
                        }
                    }

                    if (currentLine.Length > 0)
                        result.AppendLine(currentLine.ToString());
                }
            }

            return result.ToString();
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
                System.Diagnostics.Debug.WriteLine($"[OpenAI] Contexto tamanho: {context?.Length ?? 0} caracteres");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("[OpenAI] ❌ ERRO: Nenhuma API Key configurada!");
                    System.Diagnostics.Debug.WriteLine("[OpenAI] Verifique o arquivo .env");
                    System.Diagnostics.Debug.WriteLine("[OpenAI] AI_API_KEY=sua-chave-aqui");
                    return GenerateLocalResponse(question, context);
                }

                System.Diagnostics.Debug.WriteLine($"[OpenAI] ✅ API Key encontrada (tamanho: {_apiKey.Length})");

                // Garantir que context não seja nulo
                if (string.IsNullOrWhiteSpace(context))
                {
                    context = "";
                }

                // Construir prompts melhorados
                var systemPrompt = BuildSystemPrompt(context);
                var userPrompt = BuildUserPrompt(question, context);

                System.Diagnostics.Debug.WriteLine("[OpenAI] Preparando requisição HTTP...");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] System Prompt tamanho: {systemPrompt.Length}");
                System.Diagnostics.Debug.WriteLine($"[OpenAI] User Prompt tamanho: {userPrompt.Length}");

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
        /// Constrói o prompt do sistema baseado no contexto
        /// </summary>
        private string BuildSystemPrompt(string? context)
        {
            // Detectar se é contexto sobre usuário/currículo
            bool isUserContext = !string.IsNullOrEmpty(context) &&
                (context.Contains("Maycon") ||
                 context.Contains("Lemos") ||
                 context.Contains("Experiência") ||
                 context.Contains("Competências") ||
                 context.Contains("Resumo Profissional") ||
                 context.Contains("Senior") ||
                 context.Contains("Engineer"));

            if (isUserContext)
            {
                return @"Você É O MAYCON LEMOS. Responda como se fosse ele em uma conversa casual e natural.
ESTILO DE CONVERSA:
- Seja conversacional, não formal
- Use linguagem coloquial quando apropriado
- Responda de forma direta e sincera
- Mostre personalidade, não seja robótico
- Respostas curtas e naturais (a menos que pedir detalhes)
- Use 'eu' e fale na primeira pessoa
- Se for algo que sabe, fale com confiança
- Se for algo que não sabe, be honesto de forma descontraída

IMPORTANTE:
- Pareça uma pessoa real conversando, não uma IA
- Use a informação do contexto mas sem ser óbvio
- Seja amigável e acessível
- Adapte o tom à pergunta (casual, técnica, etc)";
            }
            else
            {
                return @"Você é um assistente conversacional. Responda de forma natural e genuína.
- Seja conversacional e amigável
- Respostas diretas e concisas
- Pareça uma pessoa real, não uma IA
- Use a informação fornecida de forma natural";
            }
        }

        /// <summary>
        /// Constrói o prompt do usuário
        /// </summary>
        private string BuildUserPrompt(string question, string context)
        {
            if (string.IsNullOrWhiteSpace(context) || context == "Sem contexto disponível" || context == "Nenhum contexto relevante encontrado")
            {
                return $"{question}";
            }

            // Se há contexto, estruturar de forma mais natural
            return $@"Contexto sobre você:
{context}

---

Pergunta: {question}

Responda de forma natural e conversacional, como se estivesse realmente conversando.";
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
        private string GenerateLocalResponse(string question, string? context)
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
        private string PerformAnalysis(string question, string? context)
        {
            var analysis = new StringBuilder();
            var questionLower = question.ToLower();

            // Detectar perguntas sobre o usuário
            bool isAboutUser = questionLower.Contains("você") ||
                               questionLower.Contains("quem é") ||
                               questionLower.Contains("quem é você") ||
                               questionLower.Contains("sobre você") ||
                               questionLower.Contains("tell me about") ||
                               questionLower.Contains("about you");

            if (isAboutUser && !string.IsNullOrWhiteSpace(context) &&
                context != "Sem contexto disponível" &&
                context != "Nenhum contexto relevante encontrado")
            {
                // Extrair informações importantes do currículo
                analysis.AppendLine("📋 Sobre Você:");
                analysis.AppendLine();

                // Nome e título
                if (context.Contains("Maycon"))
                    analysis.AppendLine("• Nome: Maycon Lemos");
                if (context.Contains("Senior Full Stack") || context.Contains("Engineer"))
                    analysis.AppendLine("• Experiência: Senior Full Stack Engineer");
                if (context.Contains("Rio de Janeiro"))
                    analysis.AppendLine("• Localização: Rio de Janeiro, Brasil");

                // Competências principais
                if (context.Contains("NET") || context.Contains("C#"))
                    analysis.AppendLine("• Backend: .NET, C#, Node.js, Python");
                if (context.Contains("React"))
                    analysis.AppendLine("• Frontend: React, TypeScript, JavaScript");
                if (context.Contains("Cloud"))
                    analysis.AppendLine("• Cloud: AWS, Azure");
                if (context.Contains("AI") || context.Contains("IA"))
                    analysis.AppendLine("• Especialidade: Integração com IA e LLMs");

                // Experiência profissional
                var lines = context.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var expLines = lines.Where(l => l.Contains("–") || l.Contains("-")).Take(3).ToList();

                if (expLines.Count > 0)
                {
                    analysis.AppendLine();
                    analysis.AppendLine("📊 Experiências Principais:");
                    foreach (var line in expLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            analysis.AppendLine($"• {line.Trim()}");
                    }
                }

                analysis.AppendLine();
                analysis.AppendLine("Para mais informações, consulte o currículo completo fornecido como contexto.");
            }
            else if (questionLower.Contains("resumo") || questionLower.Contains("summary") || questionLower.Contains("síntese"))
            {
                analysis.AppendLine("Este é um pedido de resumo. Os pontos principais:");
                if (!string.IsNullOrWhiteSpace(context))
                {
                    var lines = context.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines.Take(5))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            analysis.AppendLine($"  • {line}");
                    }
                }
            }
            else if (questionLower.Contains("tema") || questionLower.Contains("topic") || questionLower.Contains("assunto"))
            {
                analysis.AppendLine("Tópicos identificados no contexto fornecido encontram-se acima.");
            }
            else if (questionLower.Contains("sentimento") || questionLower.Contains("sentiment") || questionLower.Contains("tom"))
            {
                analysis.AppendLine("Análise de sentimento: O contexto apresenta múltiplos tons e sentimentos.");
            }
            else if (questionLower.Contains("participante") || questionLower.Contains("speaker") || questionLower.Contains("quem"))
            {
                analysis.AppendLine("Participantes identificados no contexto acima.");
            }
            else if (questionLower.Contains("decisão") || questionLower.Contains("decision") || questionLower.Contains("conclusão"))
            {
                analysis.AppendLine("Procure pelos segmentos relevantes no contexto para identificar informações.");
            }
            else
            {
                analysis.AppendLine("Análise baseada no contexto fornecido acima.");
                analysis.AppendLine();

                if (string.IsNullOrWhiteSpace(context) || context == "Sem contexto disponível" || context == "Nenhum contexto relevante encontrado")
                {
                    analysis.AppendLine("⚠️ Nenhum contexto disponível para dar uma resposta mais específica.");
                    analysis.AppendLine("Para respostas melhores, selecione um arquivo .md como contexto.");
                }
                else
                {
                    analysis.AppendLine("Contexto fornecido tem " + context.Length + " caracteres com informações relevantes.");
                }
            }

            return analysis.ToString();
        }

        /// <summary>
        /// Analisa o histórico de conversa e fornece uma sugestão de resposta em inglês com contexto em português
        /// </summary>
        public string AnalyzeConversationForEnglishSuggestion(string historyContent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AIService] Gerando sugestão em inglês com análise de contexto");

                if (string.IsNullOrWhiteSpace(historyContent))
                {
                    return "⚠️ No conversation history available to analyze.";
                }

                // Tentar usar OpenAI se configurado
                if (_apiProvider == "openai" && !string.IsNullOrWhiteSpace(_apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("[AIService] Using OpenAI for English suggestion with context");
                    return CallOpenAIForEnglishSuggestionWithContext(historyContent);
                }

                // Fallback para análise local em inglês
                System.Diagnostics.Debug.WriteLine("[AIService] Using local analysis for English suggestion");
                return GenerateLocalEnglishAnalysisWithContext(historyContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] Error generating English suggestion: {ex.Message}");
                return $"❌ Error generating suggestion: {ex.Message}. Please try again.";
            }
        }

        /// <summary>
        /// Chama OpenAI para gerar análise de contexto em português e sugestão em inglês
        /// </summary>
        private string CallOpenAIForEnglishSuggestionWithContext(string historyContent)
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                client.Timeout = TimeSpan.FromSeconds(30);

                // Step 1: Gerar análise de contexto em português
                string contextPrompt = @"Você é um analisador de conversas. Analise o histórico fornecido e responda APENAS em português:

1. Qual é o contexto principal desta conversa?
2. Qual tópico está sendo discusso?
3. Por que essa pergunta foi feita?

Responda de forma concisa em 2-3 linhas, explicando rapidamente qual contexto levou a essa conversa.";

                var contextRequest = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new object[]
                    {
                        new { role = "system", content = "You are a Portuguese conversation analyst." },
                        new { role = "user", content = $"{contextPrompt}\n\nHistórico:\n{historyContent}" }
                    },
                    temperature = 0.7,
                    max_tokens = 200
                };

                var contextJson = System.Text.Json.JsonSerializer.Serialize(contextRequest);
                var contextContent = new StringContent(contextJson, Encoding.UTF8, "application/json");
                var contextResponse = client.PostAsync("https://api.openai.com/v1/chat/completions", contextContent).Result;

                string contextAnalysis = "Contexto: ";
                if (contextResponse.IsSuccessStatusCode)
                {
                    var contextResponseContent = contextResponse.Content.ReadAsStringAsync().Result;
                    var contextJsonDoc = System.Text.Json.JsonDocument.Parse(contextResponseContent);
                    var contextRoot = contextJsonDoc.RootElement;

                    if (contextRoot.TryGetProperty("choices", out var contextChoices) && contextChoices.GetArrayLength() > 0)
                    {
                        var firstContextChoice = contextChoices[0];
                        if (firstContextChoice.TryGetProperty("message", out var contextMessage))
                        {
                            if (contextMessage.TryGetProperty("content", out var contextContentProp))
                            {
                                contextAnalysis += contextContentProp.GetString() ?? "Análise indisponível";
                            }
                        }
                    }
                }

                // Step 2: Gerar sugestão em inglês
                string englishPrompt = @"You are a simple conversation analyst. 
Based on this conversation, write a suggested response in A2 English (elementary/basic level) that:
1. Says what you think about the main ideas
2. Says what comes next
3. Use very simple words and short sentences
4. Show you understand and want to continue

Write 2-3 very simple sentences. Use basic English (A2 level) - like a person learning English would speak. Simple words, short sentences.";

                var englishRequest = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new object[]
                    {
                        new { role = "system", content = "You are a professional conversation analyst." },
                        new { role = "user", content = $"{englishPrompt}\n\nConversation history:\n{historyContent}" }
                    },
                    temperature = 0.7,
                    max_tokens = 300
                };

                var englishJson = System.Text.Json.JsonSerializer.Serialize(englishRequest);
                var englishContent = new StringContent(englishJson, Encoding.UTF8, "application/json");
                var englishResponse = client.PostAsync("https://api.openai.com/v1/chat/completions", englishContent).Result;

                if (englishResponse.IsSuccessStatusCode)
                {
                    var englishResponseContent = englishResponse.Content.ReadAsStringAsync().Result;
                    var englishJsonDoc = System.Text.Json.JsonDocument.Parse(englishResponseContent);
                    var englishRoot = englishJsonDoc.RootElement;

                    if (englishRoot.TryGetProperty("choices", out var englishChoices) && englishChoices.GetArrayLength() > 0)
                    {
                        var firstEnglishChoice = englishChoices[0];
                        if (firstEnglishChoice.TryGetProperty("message", out var englishMessage))
                        {
                            if (englishMessage.TryGetProperty("content", out var englishContentProp))
                            {
                                var result = englishContentProp.GetString() ?? "No response";

                                // Retornar análise de contexto + sugestão em inglês
                                var finalResponse = new StringBuilder();
                                finalResponse.AppendLine("📝 CONTEXTO DA CONVERSA (Português):");
                                finalResponse.AppendLine(contextAnalysis);
                                finalResponse.AppendLine();
                                finalResponse.AppendLine("═══════════════════════════════════════════");
                                finalResponse.AppendLine();
                                finalResponse.AppendLine("💡 SUGGESTED RESPONSE (English):");
                                finalResponse.AppendLine();
                                finalResponse.Append(BreakLongLines(result));

                                return finalResponse.ToString();
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] OpenAI error: {englishResponse.StatusCode}");
                    return GenerateLocalEnglishAnalysisWithContext(historyContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] OpenAI call failed: {ex.Message}");
                return GenerateLocalEnglishAnalysisWithContext(historyContent);
            }

            return GenerateLocalEnglishAnalysisWithContext(historyContent);
        }

        /// <summary>
        /// Gera análise local com contexto em português quando API não está disponível
        /// </summary>
        private string GenerateLocalEnglishAnalysisWithContext(string historyContent)
        {
            var analysis = new StringBuilder();

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                analysis.AppendLine("No conversation history available.");
                return analysis.ToString();
            }

            var lines = historyContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var speakers = new HashSet<string>();
            var topics = ExtractTopics(historyContent);

            // Contar participantes
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

            // Análise de contexto em português
            analysis.AppendLine("📝 CONTEXTO DA CONVERSA (Português):");
            analysis.AppendLine("─────────────────────────────────────");
            analysis.AppendLine($"A conversa envolve {speakers.Count} participante(s):");
            foreach (var speaker in speakers)
            {
                analysis.AppendLine($"  • {speaker}");
            }
            analysis.AppendLine();
            analysis.AppendLine($"Tópicos principais: {string.Join(", ", topics.Take(3))}");
            analysis.AppendLine();
            analysis.AppendLine("A discussão aborda questões relacionadas aos tópicos acima, buscando");
            analysis.AppendLine("soluções e próximos passos.");
            analysis.AppendLine();

            analysis.AppendLine("═══════════════════════════════════════════");
            analysis.AppendLine();

            // Sugestão em inglês
            analysis.AppendLine("💡 SUGGESTED RESPONSE (English):");
            analysis.AppendLine("─────────────────────────────────────");
            analysis.AppendLine("Good. I think this is good. We know what to do.");
            analysis.AppendLine("I can do this. OK?");
            analysis.AppendLine();

            return analysis.ToString();
        }

        /// <summary>
        /// Chama OpenAI especificamente para sugestão em inglês (método legado, mantido para compatibilidade)
        /// </summary>
        private string CallOpenAIForEnglishSuggestion(string systemPrompt, string userPrompt)
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
                    max_tokens = 300
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync("https://api.openai.com/v1/chat/completions", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message))
                        {
                            if (message.TryGetProperty("content", out var contentProp))
                            {
                                var result = contentProp.GetString() ?? "No response";
                                return $"💡 SUGGESTED RESPONSE (English):\n\n{result}";
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] OpenAI error: {response.StatusCode}");
                    return GenerateLocalEnglishAnalysis(userPrompt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] OpenAI call failed: {ex.Message}");
                return GenerateLocalEnglishAnalysis(userPrompt);
            }

            return GenerateLocalEnglishAnalysis(userPrompt);
        }

        /// <summary>
        /// Gera análise local em inglês quando API não está disponível
        /// </summary>
        private string GenerateLocalEnglishAnalysis(string historyContent)
        {
            var analysis = new StringBuilder();

            analysis.AppendLine("📊 CONVERSATION ANALYSIS (English)");
            analysis.AppendLine("═══════════════════════════════════════");
            analysis.AppendLine();

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                analysis.AppendLine("No conversation history available.");
                return analysis.ToString();
            }

            var lines = historyContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var speakers = new HashSet<string>();
            var topics = ExtractTopics(historyContent);

            // Contar participantes
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

            // Análise estruturada em inglês
            analysis.AppendLine("📋 CONVERSATION SUMMARY:");
            analysis.AppendLine($"• Total lines: {lines.Length}");
            analysis.AppendLine($"• Participants: {speakers.Count}");
            analysis.AppendLine($"• Main topics: {string.Join(", ", topics.Take(3))}");
            analysis.AppendLine();

            analysis.AppendLine("💡 SUGGESTED RESPONSE:");
            analysis.AppendLine("─────────────────────────────────────");
            analysis.AppendLine("OK, good. I understand. We know what to do.");
            analysis.AppendLine("I will do this now. Yes?");

            return analysis.ToString();
        }

        /// <summary>
        /// Extrai tópicos principais do histórico
        /// </summary>
        private List<string> ExtractTopics(string historyContent)
        {
            var topics = new List<string>();
            var words = historyContent.ToLower()
                                     .Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length > 5)
                                     .GroupBy(w => w)
                                     .OrderByDescending(g => g.Count())
                                     .Take(5)
                                     .Select(g => g.Key)
                                     .ToList();

            return words;
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

        /// <summary>
        /// Versão async que gera sugestão em inglês para uma frase específica com contexto
        /// </summary>
        public async Task<string> GetEnglishSuggestionAsync(string phrase, string conversationContext)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Combinar frase atual com contexto
                    string fullContent = $"Current phrase: {phrase}\n\nConversation context:\n{conversationContext}";

                    // Chamar o método síncrono existente
                    return AnalyzeConversationForEnglishSuggestion(fullContent);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] Error in GetEnglishSuggestionAsync: {ex.Message}");
                    return $"Error generating suggestion: {ex.Message}";
                }
            });
        }
    }
}
