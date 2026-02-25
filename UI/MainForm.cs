using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TraducaoTIME.Utils;

namespace TraducaoTIME.UI
{
    public class MainForm : Form
    {
        private MenuStrip? menuStrip;
        private ToolStripMenuItem? configMenu;
        private ToolStripMenuItem? iaMenu;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;
        private Panel? containerPanel;
        private RichTextBox? conversationTextBox;
        private Panel? buttonPanel;
        private Button? buttonIniciar;
        private Button? buttonParar;

        private System.Threading.Thread? transcriptionThread;
        private bool isTranscribing = false;

        // Histórico de linhas finalizadas
        private List<string> _finalizedLines = new List<string>();
        // Texto interim atual (cresce palavra por palavra)
        private string _currentInterimText = "";

        // Referência para a janela IA
        private AIForm? _iaForm;

        // Gerenciador de histórico em arquivo TXT
        private HistoryManager? _historyManager;

        public MainForm()
        {
            // Configurações básicas da janela
            this.Text = "Tradução TIME";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new System.Drawing.Size(400, 300);

            // Inicializar gerenciador de histórico
            _historyManager = HistoryManager.Instance;

            // Adicionar margens nas laterais para não cortar o conteúdo


            // Criar menu
            CreateMenu();

            // Criar rodapé (adicionar antes do container para que fique no topo da ordem Z)
            CreateStatusBar();

            // Criar painel contêiner
            CreateContainerPanel();

            // Se inscrever no evento de mudança de configurações
            AppConfig.Instance.ConfigChanged += (sender, e) => AtualizarStatus();

            // Atualizar status inicial
            AtualizarStatus();
        }

        private void CreateMenu()
        {
            // Criar MenuStrip
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;

            // Criar item de menu CONFIG
            configMenu = new ToolStripMenuItem("CONFIG");
            configMenu.Click += ConfigMenu_Click!;

            // Criar item de menu IA
            iaMenu = new ToolStripMenuItem("IA");
            iaMenu.Click += IAMenu_Click!;

            // Adicionar o menu ao MenuStrip
            if (menuStrip != null)
            {
                menuStrip.Items.Add(configMenu);
                menuStrip.Items.Add(iaMenu);

                // Adicionar MenuStrip ao formulário
                this.MainMenuStrip = menuStrip;
                this.Controls.Add(menuStrip);
            }
        }

        private void CreateContainerPanel()
        {
            // Criar painel que ocupará espaço entre menu e status bar
            containerPanel = new Panel();
            containerPanel.Dock = DockStyle.Fill;
            containerPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);


            // Criar painel com botões de controle
            CreateButtonPanel();

            // Adicionar conteúdo de conversa
            CreateConversationContent();

            if (containerPanel != null)
                this.Controls.Add(containerPanel);
        }

        private void CreateButtonPanel()
        {
            // Painel superior para botões
            buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Height = 50;
            buttonPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            buttonPanel.Padding = new Padding(10);

            // Botão Iniciar
            buttonIniciar = new Button();
            buttonIniciar.Text = "Iniciar Transcrição";
            buttonIniciar.Location = new System.Drawing.Point(10, 10);
            buttonIniciar.Width = 150;
            buttonIniciar.Height = 30;
            buttonIniciar.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            buttonIniciar.ForeColor = System.Drawing.Color.White;
            buttonIniciar.Click += ButtonIniciar_Click!;
            buttonPanel.Controls.Add(buttonIniciar);

            // Botão Parar
            buttonParar = new Button();
            buttonParar.Text = "Parar Transcrição";
            buttonParar.Location = new System.Drawing.Point(170, 10);
            buttonParar.Width = 150;
            buttonParar.Height = 30;
            buttonParar.BackColor = System.Drawing.Color.FromArgb(200, 55, 55);
            buttonParar.ForeColor = System.Drawing.Color.White;
            buttonParar.Click += ButtonParar_Click!;
            buttonParar.Enabled = false;
            buttonPanel.Controls.Add(buttonParar);

            if (containerPanel != null)
                containerPanel.Controls.Add(buttonPanel);
        }

        private void CreateConversationContent()
        {
            // Criar painel contêiner com padding
            Panel paddingPanel = new Panel();
            paddingPanel.Dock = DockStyle.Fill;
            paddingPanel.Padding = new Padding(0, 30, 0, 30); // padding top e bottom
            paddingPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);

            // RichTextBox para exibir transcrições (Google Meet, Teams, etc)
            conversationTextBox = new RichTextBox();
            conversationTextBox.Dock = DockStyle.Fill;
            conversationTextBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            conversationTextBox.ForeColor = System.Drawing.Color.White;
            conversationTextBox.BorderStyle = BorderStyle.None;
            conversationTextBox.Font = new System.Drawing.Font("Arial", 10);
            conversationTextBox.ReadOnly = true;
            conversationTextBox.WordWrap = true;

            // Placeholder inicial
            conversationTextBox.Text = "Aguardando transcrição...\r\n";

            paddingPanel.Controls.Add(conversationTextBox);
            if (containerPanel != null)
                containerPanel.Controls.Add(paddingPanel);
        }

        private void CreateStatusBar()
        {
            // Criar StatusStrip
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "";

            if (statusStrip != null)
            {
                statusStrip.Items.Add(statusLabel);
                // Adicionar ao formulário
                this.Controls.Add(statusStrip);
            }
        }

        private void ConfigMenu_Click(object sender, EventArgs e)
        {
            // Abrir janela de configuração
            ConfigForm configForm = new ConfigForm();
            configForm.ShowDialog(this);

            // Atualizar status após fechar a janela de configuração
            AtualizarStatus();
        }

        private void IAMenu_Click(object sender, EventArgs e)
        {
            // Obter histórico de conversa para passar para a IA
            string conversationHistory = GetConversationHistory();

            // Abrir a janela IA
            if (_iaForm == null || _iaForm.IsDisposed)
            {
                _iaForm = new AIForm(conversationHistory);
                _iaForm.Show(this);
            }
            else
            {
                // Se já existe, atualizar o histórico e trazer para frente
                _iaForm.UpdateConversationHistory(conversationHistory);
                _iaForm.BringToFront();
                _iaForm.Focus();
            }
        }

        private string GetConversationHistory()
        {
            // Reconstruir o histórico completo da conversa
            StringBuilder history = new StringBuilder();

            // Adicionar linhas finalizadas
            foreach (var line in _finalizedLines)
            {
                history.AppendLine(line);
            }

            // Adicionar interim atual se houver
            if (!string.IsNullOrWhiteSpace(_currentInterimText))
            {
                history.AppendLine(_currentInterimText);
            }

            return history.ToString();
        }

        public void ShowTranslation(string transcription)
        {
            // Converter para TranscriptionSegment e chamar o método melhorado
            var segment = new TranscriptionSegment(transcription, isFinal: true);
            ShowTranslation(segment);
        }

        public void ShowTranslation(TranscriptionSegment segment)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowTranslation chamado com: '{segment.Text}' (Final: {segment.IsFinal})");
            Console.WriteLine($"[UI] Interim={!segment.IsFinal} | Text='{segment.Text}' | Finalized antes={_finalizedLines.Count}");

            // Thread-safe: garantir que a atualização aconteça na thread da UI
            if (conversationTextBox?.InvokeRequired == true)
            {
                conversationTextBox.Invoke(new Action(() => ShowTranslation(segment)));
                return;
            }

            if (segment.IsFinal)
            {
                // FINAL: Adicionar o texto confirmado às linhas finalizadas
                if (!string.IsNullOrWhiteSpace(segment.Text))
                {
                    // Incluir o speaker se disponível
                    string textToAdd = !string.IsNullOrWhiteSpace(segment.Speaker)
                        ? $"{segment.Speaker}: {segment.Text}"
                        : segment.Text;

                    _finalizedLines.Add(textToAdd);
                    Console.WriteLine($"[UI] [FINAL] ADICIONADO: '{textToAdd}' | Total linhas: {_finalizedLines.Count}");

                    // Salvar no arquivo de histórico
                    if (_historyManager != null)
                    {
                        string speaker = !string.IsNullOrWhiteSpace(segment.Speaker) ? segment.Speaker : "Participante";
                        _historyManager.AddMessage(speaker, segment.Text);
                    }
                }

                // Limpar interim para a próxima frase
                _currentInterimText = "";
                Console.WriteLine($"[UI] Interim limpo, pronto para nova frase");
            }
            else
            {
                // INTERIM: Atualizar o texto que está crescendo
                if (!string.IsNullOrWhiteSpace(segment.Text))
                {
                    // Incluir o speaker se disponível
                    _currentInterimText = !string.IsNullOrWhiteSpace(segment.Speaker)
                        ? $"{segment.Speaker}: {segment.Text}"
                        : segment.Text;

                    Console.WriteLine($"[UI] [INTERIM] ATUALIZADO: '{_currentInterimText}'");
                }
            }

            // Reconstruir a exibição
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (conversationTextBox == null) return;

            // Construir o texto completo com quebras de linhas claras e visuais
            StringBuilder displayText = new StringBuilder();

            // Adicionar linhas finalizadas
            for (int i = 0; i < _finalizedLines.Count; i++)
            {
                if (i > 0)
                    displayText.Append("\r\n");

                // Usar prefixo simples em ASCII para finalizadas
                displayText.Append($"• {_finalizedLines[i]}");
            }

            // Adicionar separador e interim (se houver) em nova linha clara
            if (!string.IsNullOrWhiteSpace(_currentInterimText))
            {
                // Se há linhas finalizadas, adicionar quebra de linha e separador visual
                if (_finalizedLines.Count > 0)
                {
                    displayText.Append("\r\n");
                    displayText.Append("────────────────────────────────────────\r\n");
                }
                else if (displayText.Length > 0)
                    displayText.Append("\r\n");

                // Usar prefixo simples para interim
                displayText.Append($"» {_currentInterimText}");
            }

            // DEBUG
            Console.WriteLine($"[RefreshDisplay] Finalizadas: {_finalizedLines.Count} | Interim: {!string.IsNullOrWhiteSpace(_currentInterimText)}");
            Console.WriteLine($"[RefreshDisplay] Atualizando display com {_finalizedLines.Count} linhas finalizadas");

            // Atualizar o texto do RichTextBox
            conversationTextBox.Text = displayText.ToString();

            // Agora formatar as cores
            FormatDisplay();

            // Ir para o final do texto para acompanhar a transcrição
            conversationTextBox.SelectionStart = conversationTextBox.Text.Length;
            conversationTextBox.ScrollToCaret();
        }

        private void FormatDisplay()
        {
            if (conversationTextBox == null) return;

            // Começar tudo em branco normal
            conversationTextBox.SelectAll();
            conversationTextBox.SelectionColor = System.Drawing.Color.White;
            conversationTextBox.SelectionFont = new System.Drawing.Font(
                conversationTextBox.Font.FontFamily,
                conversationTextBox.Font.Size,
                System.Drawing.FontStyle.Regular
            );

            // Formatar linhas finalizadas (branco brilhante + normal) - cada uma em sua linha
            int currentPos = 0;
            for (int i = 0; i < _finalizedLines.Count; i++)
            {
                string lineText = $"• {_finalizedLines[i]}";
                int lineStartPos = conversationTextBox.Text.IndexOf(lineText, currentPos);

                if (lineStartPos >= 0)
                {
                    // Formatar o prefixo (color lightgray)
                    int prefixLen = 2; // "• " = 2 caracteres
                    conversationTextBox.Select(lineStartPos, prefixLen);
                    conversationTextBox.SelectionColor = System.Drawing.Color.LightGray;
                    conversationTextBox.SelectionFont = new System.Drawing.Font(
                        conversationTextBox.Font.FontFamily,
                        conversationTextBox.Font.Size,
                        System.Drawing.FontStyle.Regular
                    );

                    // Formatar o texto em branco
                    conversationTextBox.Select(lineStartPos + prefixLen, lineText.Length - prefixLen);
                    conversationTextBox.SelectionColor = System.Drawing.Color.White;
                    conversationTextBox.SelectionFont = new System.Drawing.Font(
                        conversationTextBox.Font.FontFamily,
                        conversationTextBox.Font.Size,
                        System.Drawing.FontStyle.Regular
                    );

                    currentPos = lineStartPos + lineText.Length;
                }
            }

            // Formatar separador visual (se houver interim)
            if (!string.IsNullOrWhiteSpace(_currentInterimText))
            {
                string separator = "────────────────────────────────────────";
                int separatorPos = conversationTextBox.Text.IndexOf(separator);
                if (separatorPos >= 0)
                {
                    conversationTextBox.Select(separatorPos, separator.Length);
                    conversationTextBox.SelectionColor = System.Drawing.Color.DarkGray;
                }
            }

            // Formatar interim (amarelo brilhante + itálico) - em sua própria linha
            if (!string.IsNullOrWhiteSpace(_currentInterimText))
            {
                string interimText = $"» {_currentInterimText}";
                int interimPos = conversationTextBox.Text.LastIndexOf(interimText);

                if (interimPos >= 0)
                {
                    // Formatar o prefixo em amarelo
                    int prefixLen = 2; // "» " = 2 caracteres
                    conversationTextBox.Select(interimPos, prefixLen);
                    conversationTextBox.SelectionColor = System.Drawing.Color.Yellow;
                    conversationTextBox.SelectionFont = new System.Drawing.Font(
                        conversationTextBox.Font.FontFamily,
                        conversationTextBox.Font.Size,
                        System.Drawing.FontStyle.Bold
                    );

                    // Formatar o texto em cor laranja itálico
                    conversationTextBox.Select(interimPos + prefixLen, interimText.Length - prefixLen);
                    conversationTextBox.SelectionColor = System.Drawing.Color.Gold;
                    conversationTextBox.SelectionFont = new System.Drawing.Font(
                        conversationTextBox.Font.FontFamily,
                        conversationTextBox.Font.Size,
                        System.Drawing.FontStyle.Italic
                    );
                }
            }

            // Resetar seleção
            conversationTextBox.Select(conversationTextBox.Text.Length, 0);
        }

        private void AtualizarStatus()
        {
            try
            {
                string opcao = AppConfig.Instance.SelectedOption;
                string descricaoOpcao = "";

                switch (opcao)
                {
                    case "1":
                        descricaoOpcao = "Transcrição SEM diarização";
                        break;
                    case "2":
                        descricaoOpcao = "Transcrição COM diarização";
                        break;
                    case "3":
                        descricaoOpcao = "Apenas capturar áudio";
                        break;
                    default:
                        descricaoOpcao = "Nenhuma";
                        break;
                }

                string dispositivo = !string.IsNullOrWhiteSpace(AppConfig.Instance.SelectedDeviceName)
                    ? AppConfig.Instance.SelectedDeviceName
                    : "Não selecionado";

                if (statusLabel != null)
                    statusLabel.Text = $"Modo: {descricaoOpcao} | Dispositivo: {dispositivo}";
            }
            catch (Exception ex)
            {
                if (statusLabel != null)
                    statusLabel.Text = $"Erro ao atualizar status: {ex.Message}";
            }
        }

        public void SetTranscriptionCallbacks(Action<NAudio.CoreAudioApi.MMDevice> semDiarizacao, Func<NAudio.CoreAudioApi.MMDevice, System.Threading.Tasks.Task> comDiarizacao)
        {
            // Armazenar os callbacks para uso posterior
        }

        private void ButtonIniciar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AppConfig.Instance.SelectedDeviceName))
            {
                MessageBox.Show("Dispositivo não selecionado! Configure em CONFIG primeiro.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isTranscribing = true;
            if (buttonIniciar != null) buttonIniciar.Enabled = false;
            if (buttonParar != null) buttonParar.Enabled = true;

            // Limpar histórico e texto anterior
            _finalizedLines.Clear();
            _currentInterimText = "";
            if (conversationTextBox != null)
                conversationTextBox.Clear();

            // Executar transcrição em thread separada
            transcriptionThread = new System.Threading.Thread(() =>
            {
                try
                {
                    string opcao = AppConfig.Instance.SelectedOption;
                    var device = AppConfig.Instance.GetSelectedDevice();

                    if (opcao == "1")
                    {
                        TraducaoTIME.Features.TranscricaoSemDiarizacao.TranscricaoSemDiarizacao.Executar(device);
                    }
                    else if (opcao == "2")
                    {
                        TraducaoTIME.Features.TranscricaoComDiarizacao.TranscricaoComDiarizacao.Executar(device).Wait();
                    }
                    else if (opcao == "3")
                    {
                        TraducaoTIME.Features.CapturaAudio.CapturaAudio.Executar(device);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao iniciar transcrição: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    isTranscribing = false;
                    this.Invoke(new Action(() =>
                    {
                        if (buttonIniciar != null) buttonIniciar.Enabled = true;
                        if (buttonParar != null) buttonParar.Enabled = false;
                    }));
                }
            });

            transcriptionThread.IsBackground = true;
            transcriptionThread.Start();
        }

        private void ButtonParar_Click(object sender, EventArgs e)
        {
            isTranscribing = false;
            if (buttonIniciar != null) buttonIniciar.Enabled = true;
            if (buttonParar != null) buttonParar.Enabled = false;

            // Parar as transcrições
            TraducaoTIME.Features.TranscricaoSemDiarizacao.TranscricaoSemDiarizacao.Parar();
            TraducaoTIME.Features.TranscricaoComDiarizacao.TranscricaoComDiarizacao.Parar();
            TraducaoTIME.Features.CapturaAudio.CapturaAudio.Parar();

            // Tentar parar a thread se estiver rodando
            if (transcriptionThread != null && transcriptionThread.IsAlive)
            {
                transcriptionThread.Interrupt();
            }
        }
    }
}
