using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TraducaoTIME.Utils;

namespace TraducaoTIME.UI
{
    public class AIForm : Form
    {
        private Panel? topPanel;
        private Label? questionLabel;
        private TextBox? questionTextBox;
        private Button? askButton;
        private Button? analyzeHistoryButton;
        private CheckBox? enableRAGCheckBox;
        private ComboBox? mdContextComboBox;
        private Button? loadMdFilesButton;
        private Label? mdContextLabel;
        private Panel? dividerPanel;
        private Panel? responsePanel;
        private RichTextBox? responseTextBox;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;

        // Referência para histórico de conversa
        private string? conversationHistory;

        public AIForm(string conversationHistory)
        {
            this.conversationHistory = conversationHistory;

            // Configurações básicas da janela
            this.Text = "Assistente IA - Análise de Conversa";
            this.Width = 900;
            this.Height = 700;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);

            // Criar componentes
            CreateTopPanel();
            CreateDivider();
            CreateResponsePanel();
            CreateStatusBar();
        }

        private void CreateTopPanel()
        {
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 120;
            topPanel.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            topPanel.Padding = new Padding(15);

            // Label "Pergunta"
            questionLabel = new Label();
            questionLabel.Text = "Faça uma pergunta sobre a conversa:";
            questionLabel.ForeColor = System.Drawing.Color.White;
            questionLabel.Font = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            questionLabel.Location = new System.Drawing.Point(15, 15);
            questionLabel.Width = 300;
            questionLabel.Height = 20;
            topPanel.Controls.Add(questionLabel);

            // TextBox para pergunta
            questionTextBox = new TextBox();
            questionTextBox.Location = new System.Drawing.Point(15, 40);
            questionTextBox.Width = 650;
            questionTextBox.Height = 30;
            questionTextBox.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            questionTextBox.ForeColor = System.Drawing.Color.White;
            questionTextBox.BorderStyle = BorderStyle.FixedSingle;
            questionTextBox.Font = new System.Drawing.Font("Arial", 10);
            questionTextBox.Multiline = false;
            topPanel.Controls.Add(questionTextBox);

            // Botão Perguntar
            askButton = new Button();
            askButton.Text = "Perguntar";
            askButton.Location = new System.Drawing.Point(680, 40);
            askButton.Width = 100;
            askButton.Height = 30;
            askButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            askButton.ForeColor = System.Drawing.Color.White;
            askButton.FlatStyle = FlatStyle.Flat;
            askButton.Click += AskButton_Click!;
            topPanel.Controls.Add(askButton);

            // Botão Analisar Histórico
            analyzeHistoryButton = new Button();
            analyzeHistoryButton.Text = "📄 Analisar TXT";
            analyzeHistoryButton.Location = new System.Drawing.Point(790, 40);
            analyzeHistoryButton.Width = 100;
            analyzeHistoryButton.Height = 30;
            analyzeHistoryButton.BackColor = System.Drawing.Color.FromArgb(0, 150, 100);
            analyzeHistoryButton.ForeColor = System.Drawing.Color.White;
            analyzeHistoryButton.FlatStyle = FlatStyle.Flat;
            analyzeHistoryButton.Click += AnalyzeHistoryButton_Click!;
            topPanel.Controls.Add(analyzeHistoryButton);

            // CheckBox para RAG
            enableRAGCheckBox = new CheckBox();
            enableRAGCheckBox.Text = "Ativar RAG (Buscar contexto na conversa)";
            enableRAGCheckBox.Location = new System.Drawing.Point(15, 80);
            enableRAGCheckBox.Width = 400;
            enableRAGCheckBox.Height = 20;
            enableRAGCheckBox.ForeColor = System.Drawing.Color.White;
            enableRAGCheckBox.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            enableRAGCheckBox.Checked = true;
            topPanel.Controls.Add(enableRAGCheckBox);

            // Label para contexto .md
            mdContextLabel = new Label();
            mdContextLabel.Text = "Contexto .md:";
            mdContextLabel.ForeColor = System.Drawing.Color.White;
            mdContextLabel.Font = new System.Drawing.Font("Arial", 9);
            mdContextLabel.Location = new System.Drawing.Point(420, 80);
            mdContextLabel.Width = 100;
            mdContextLabel.Height = 20;
            topPanel.Controls.Add(mdContextLabel);

            // ComboBox para selecionar arquivo .md
            mdContextComboBox = new ComboBox();
            mdContextComboBox.Location = new System.Drawing.Point(520, 78);
            mdContextComboBox.Width = 150;
            mdContextComboBox.Height = 25;
            mdContextComboBox.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            mdContextComboBox.ForeColor = System.Drawing.Color.White;
            mdContextComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            topPanel.Controls.Add(mdContextComboBox);

            // Botão para recarregar arquivos .md
            loadMdFilesButton = new Button();
            loadMdFilesButton.Text = "Recarregar";
            loadMdFilesButton.Location = new System.Drawing.Point(680, 76);
            loadMdFilesButton.Width = 100;
            loadMdFilesButton.Height = 25;
            loadMdFilesButton.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
            loadMdFilesButton.ForeColor = System.Drawing.Color.White;
            loadMdFilesButton.FlatStyle = FlatStyle.Flat;
            loadMdFilesButton.Click += LoadMdFilesButton_Click!;
            topPanel.Controls.Add(loadMdFilesButton);

            this.Controls.Add(topPanel);

            // Carregar lista inicial de arquivos .md
            LoadMdFiles();
        }

        private void CreateDivider()
        {
            dividerPanel = new Panel();
            dividerPanel.Dock = DockStyle.Top;
            dividerPanel.Height = 1;
            dividerPanel.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.Controls.Add(dividerPanel);
        }

        private void CreateResponsePanel()
        {
            responsePanel = new Panel();
            responsePanel.Dock = DockStyle.Fill;
            responsePanel.Padding = new Padding(15);
            responsePanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);

            // Label para respostas
            Label responseLabel = new Label();
            responseLabel.Text = "Respostas da IA:";
            responseLabel.ForeColor = System.Drawing.Color.White;
            responseLabel.Font = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            responseLabel.Dock = DockStyle.Top;
            responseLabel.Height = 25;
            responsePanel.Controls.Add(responseLabel);

            // RichTextBox para respostas
            responseTextBox = new RichTextBox();
            responseTextBox.Dock = DockStyle.Fill;
            responseTextBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            responseTextBox.ForeColor = System.Drawing.Color.White;
            responseTextBox.BorderStyle = BorderStyle.None;
            responseTextBox.Font = new System.Drawing.Font("Arial", 10);
            responseTextBox.ReadOnly = true;
            responseTextBox.WordWrap = true;
            responseTextBox.Text = "Aguardando perguntas...\r\n";
            responsePanel.Controls.Add(responseTextBox);

            this.Controls.Add(responsePanel);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "Histórico de conversa: CARREGADO";
            statusLabel.ForeColor = System.Drawing.Color.White;

            if (statusStrip != null)
            {
                statusStrip.Items.Add(statusLabel);
                this.Controls.Add(statusStrip);
            }
        }

        private void AskButton_Click(object sender, EventArgs e)
        {
            if (questionTextBox == null || string.IsNullOrWhiteSpace(questionTextBox.Text))
            {
                MessageBox.Show("Por favor, digite uma pergunta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string question = questionTextBox.Text;
            bool useRAG = enableRAGCheckBox?.Checked ?? false;

            try
            {
                if (statusLabel != null)
                    statusLabel.Text = "Processando pergunta...";

                // Simular análise de conversa
                string response = AnalyzeConversation(question, useRAG, conversationHistory ?? "");

                // Exibir resposta
                AppendResponse(question, response);

                // Limpar campo de pergunta
                if (questionTextBox != null)
                    questionTextBox.Clear();

                if (statusLabel != null)
                    statusLabel.Text = "Resposta gerada com sucesso";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar pergunta: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (statusLabel != null)
                    statusLabel.Text = $"Erro: {ex.Message}";
            }
        }

        private void AnalyzeHistoryButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (statusLabel != null)
                    statusLabel.Text = "Analisando histórico de conversa...";

                // Obter o conteúdo do arquivo de histórico
                string historyContent = HistoryManager.Instance.GetFullHistory();

                if (string.IsNullOrWhiteSpace(historyContent))
                {
                    MessageBox.Show("Nenhum histórico de conversa disponível.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Extrair apenas perguntas
                string questions = ExtractQuestions(historyContent);

                // Montar resposta simplificada
                StringBuilder fullAnalysis = new StringBuilder();
                fullAnalysis.AppendLine("❓ PERGUNTAS/QUESTÕES LEVANTADAS:");
                fullAnalysis.AppendLine("═══════════════════════════════════════════════════════");
                fullAnalysis.AppendLine();

                if (string.IsNullOrWhiteSpace(questions))
                {
                    fullAnalysis.AppendLine("Nenhuma pergunta específica foi encontrada na conversa.");
                }
                else
                {
                    fullAnalysis.Append(questions);
                }

                fullAnalysis.AppendLine();
                fullAnalysis.AppendLine("═══════════════════════════════════════════════════════");
                fullAnalysis.AppendLine("💡 SUGESTÃO DE RESPOSTA (English)");
                fullAnalysis.AppendLine("═══════════════════════════════════════════════════════");
                fullAnalysis.AppendLine();

                // Gerar sugestão de resposta em inglês usando a IA
                string prompt = "Analyze the following conversation transcript and provide a suggested response in English that would continue or summarize this conversation professionally. Keep it concise and relevant. Maximum 3-4 sentences.";
                string analysis = GerateEnglishSuggestion(historyContent, prompt);
                fullAnalysis.Append(analysis);

                // Exibir análise simplificada
                AppendResponse("📄 ANÁLISE DO HISTÓRICO", fullAnalysis.ToString());

                // Salvar análise no arquivo de histórico
                HistoryManager.Instance.AddAnalysis(fullAnalysis.ToString());

                if (statusLabel != null)
                    statusLabel.Text = "Análise concluída e salva no histórico";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao analisar histórico: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (statusLabel != null)
                    statusLabel.Text = $"Erro: {ex.Message}";
            }
        }

        private string ExtractQuestions(string historyContent)
        {
            var lines = historyContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var questions = new StringBuilder();
            var foundQuestions = false;

            // Procurar por linhas com interrogação
            var questionLines = lines.Where(l => l.Contains("?") && !l.Contains("═") && l.Length > 5).ToList();

            if (questionLines.Count > 0)
            {
                foundQuestions = true;
                foreach (var q in questionLines)
                {
                    questions.AppendLine($"  • {q}");
                }
            }

            return foundQuestions ? questions.ToString() : "";
        }

        private string GerateEnglishSuggestion(string historyContent, string systemPrompt)
        {
            try
            {
                if (statusLabel != null)
                    statusLabel.Text = "Gerando sugestão com IA...";

                // Usar o novo método específico do AIService para análise em inglês
                var response = TraducaoTIME.Utils.AIService.Instance.AnalyzeConversationForEnglishSuggestion(historyContent);

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIForm] Erro ao gerar sugestão: {ex.Message}");
                return $"❌ Erro ao gerar sugestão: {ex.Message}\r\n\r\nVerifique seu histórico e tente novamente.";
            }
        }

        private void LoadMdFilesButton_Click(object sender, EventArgs e)
        {
            LoadMdFiles();
            if (statusLabel != null)
                statusLabel.Text = "Arquivos .md recarregados";
        }

        private void LoadMdFiles()
        {
            if (mdContextComboBox == null) return;

            mdContextComboBox.Items.Clear();
            mdContextComboBox.Items.Add("(Nenhum contexto)");

            // Obter caminho correto da pasta Contextos
            string contextPath = GetContextsPath();

            System.Diagnostics.Debug.WriteLine($"[AIForm] Procurando por contextosem: {contextPath}");

            try
            {
                if (System.IO.Directory.Exists(contextPath))
                {
                    var mdFiles = System.IO.Directory.GetFiles(contextPath, "*.md");
                    foreach (var file in mdFiles)
                    {
                        mdContextComboBox.Items.Add(System.IO.Path.GetFileName(file));
                    }
                    System.Diagnostics.Debug.WriteLine($"[AIForm] {mdFiles.Length} arquivos .md carregados");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIForm] Pasta Contextos não encontrada em: {contextPath}");
                    System.Diagnostics.Debug.WriteLine($"[AIForm] AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIForm] Erro ao carregar arquivos .md: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIForm] Stack: {ex.StackTrace}");
            }

            mdContextComboBox.SelectedIndex = 0;
        }

        private string GetContextsPath()
        {
            // Tentar múltiplos caminhos possíveis
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Caminho 1: Se estiver em bin/Debug/net9.0-windows/
            string fullPath1 = System.IO.Path.Combine(baseDir, "..", "..", "..", "Contextos");
            fullPath1 = System.IO.Path.GetFullPath(fullPath1);

            // Caminho 2: Se estiver em bin/Debug/
            string fullPath2 = System.IO.Path.Combine(baseDir, "..", "..", "Contextos");
            fullPath2 = System.IO.Path.GetFullPath(fullPath2);

            // Caminho 3: Se estiver na raiz do projeto
            string fullPath3 = System.IO.Path.Combine(baseDir, "Contextos");
            fullPath3 = System.IO.Path.GetFullPath(fullPath3);

            System.Diagnostics.Debug.WriteLine($"[AIForm] Tentando caminhos:");
            System.Diagnostics.Debug.WriteLine($"  1. {fullPath1} -> {System.IO.Directory.Exists(fullPath1)}");
            System.Diagnostics.Debug.WriteLine($"  2. {fullPath2} -> {System.IO.Directory.Exists(fullPath2)}");
            System.Diagnostics.Debug.WriteLine($"  3. {fullPath3} -> {System.IO.Directory.Exists(fullPath3)}");

            if (System.IO.Directory.Exists(fullPath1)) return fullPath1;
            if (System.IO.Directory.Exists(fullPath2)) return fullPath2;
            if (System.IO.Directory.Exists(fullPath3)) return fullPath3;

            // Fallback: retornar o primeiro (será reportado como não encontrado)
            return fullPath1;
        }

        private string GetMdContextContent()
        {
            if (mdContextComboBox == null || mdContextComboBox.SelectedItem == null)
                return "";

            string selectedFile = mdContextComboBox.SelectedItem.ToString() ?? "";
            if (selectedFile == "(Nenhum contexto)" || string.IsNullOrWhiteSpace(selectedFile))
                return "";

            string contextPath = System.IO.Path.Combine(GetContextsPath(), selectedFile);

            try
            {
                if (System.IO.File.Exists(contextPath))
                {
                    return System.IO.File.ReadAllText(contextPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIForm] Arquivo não encontrado: {contextPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIForm] Erro ao ler arquivo .md: {ex.Message}");
            }

            return "";
        }

        private string AnalyzeConversation(string question, bool useRAG, string conversationHistory)
        {
            // Aqui você pode integrar com um serviço de IA real (OpenAI, Azure OpenAI, etc.)
            // Por enquanto, vou criar uma resposta baseada em análise de contexto

            StringBuilder response = new StringBuilder();

            if (useRAG)
            {
                // Buscar contexto relevante na conversa
                var relevantSegments = ExtractRelevantSegments(question, conversationHistory);
                response.AppendLine("📊 Análise com RAG (Contexto extraído):");
                response.AppendLine();

                if (relevantSegments.Count > 0)
                {
                    response.AppendLine("Contexto encontrado:");
                    foreach (var segment in relevantSegments)
                    {
                        response.AppendLine($"  • {segment}");
                    }
                    response.AppendLine();
                }
                else
                {
                    response.AppendLine("Nenhum contexto relevante encontrado na conversa.");
                    response.AppendLine();
                }
            }

            // Análise da pergunta
            response.AppendLine("🤖 Análise:");
            response.AppendLine($"Pergunta: {question}");
            response.AppendLine();

            // Gerar resposta baseada na pergunta
            response.Append(GenerateAIResponse(question, useRAG, conversationHistory));

            return response.ToString();
        }

        private List<string> ExtractRelevantSegments(string question, string conversationHistory)
        {
            var segments = new List<string>();

            if (string.IsNullOrWhiteSpace(conversationHistory))
                return segments;

            // Palavras-chave da pergunta
            var keywords = question.ToLower().Split(new[] { ' ', ',', '.', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var conversationLines = conversationHistory.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in conversationLines)
            {
                foreach (var keyword in keywords)
                {
                    if (line.ToLower().Contains(keyword) && line.Length > 10)
                    {
                        segments.Add(line);
                        break;
                    }
                }

                if (segments.Count >= 5) break; // Limitar a 5 segmentos
            }

            return segments;
        }

        private string GenerateAIResponse(string question, bool useRAG, string conversationHistory)
        {
            // Chamar AIService para gerar resposta inteligente
            try
            {
                // Obter contexto do arquivo .md selecionado
                string mdContext = GetMdContextContent();
                string fullContext = conversationHistory;

                if (!string.IsNullOrWhiteSpace(mdContext))
                {
                    fullContext = $"CONTEXTO DO ARQUIVO .MD:\n{mdContext}\n\n---\n\nCONTEXTO DA CONVERSA:\n{conversationHistory}";
                    System.Diagnostics.Debug.WriteLine($"[AIForm] Contexto .md adicionado ({mdContext.Length} caracteres)");
                }

                if (useRAG)
                {
                    System.Diagnostics.Debug.WriteLine("[AIForm] Chamando AIService com RAG");
                    var response = TraducaoTIME.Utils.AIService.Instance.AnalyzeConversationWithRAG(question, fullContext);
                    System.Diagnostics.Debug.WriteLine($"[AIForm] Resposta recebida do AIService: {response.Substring(0, Math.Min(100, response.Length))}");
                    return response;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AIForm] Chamando análise local");
                    // Quando RAG desativado, ainda usar AIService para análise local
                    var response = TraducaoTIME.Utils.AIService.Instance.AnalyzeConversationWithRAG(question, mdContext);
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIForm] Erro ao chamar AIService: {ex.Message}");
                // Fallback para resposta genérica
                return $"❌ Erro ao gerar resposta: {ex.Message}\r\n\r\nTente novamente ou verifique o console para mais detalhes.";
            }
        }

        private void AppendResponse(string question, string response)
        {
            if (responseTextBox == null) return;

            StringBuilder fullResponse = new StringBuilder();

            if (!responseTextBox.Text.Contains("Aguardando"))
            {
                fullResponse.Append(responseTextBox.Text);
                fullResponse.AppendLine();
                fullResponse.AppendLine("═══════════════════════════════════════════════════════");
                fullResponse.AppendLine();
            }
            else
            {
                responseTextBox.Clear();
            }

            fullResponse.AppendLine($"[{DateTime.Now:HH:mm:ss}] ❓ Pergunta: {question}");
            fullResponse.AppendLine();
            fullResponse.AppendLine(response);
            fullResponse.AppendLine();

            responseTextBox.Text = fullResponse.ToString();

            // Ir para o final
            responseTextBox.SelectionStart = responseTextBox.Text.Length;
            responseTextBox.ScrollToCaret();
        }

        public void UpdateConversationHistory(string newHistory)
        {
            conversationHistory = newHistory;
            if (statusLabel != null)
                statusLabel.Text = "Histórico de conversa atualizado";
        }
    }
}
