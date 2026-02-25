using System;
using System.Windows.Forms;
using TraducaoTIME.Utils;

namespace TraducaoTIME.UI
{
    public class MainForm : Form
    {
        private MenuStrip? menuStrip;
        private ToolStripMenuItem? configMenu;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;
        private Panel? containerPanel;
        private RichTextBox? conversationTextBox;
        private Panel? buttonPanel;
        private Button? buttonIniciar;
        private Button? buttonParar;
        
        private System.Threading.Thread? transcriptionThread;
        private bool isTranscribing = false;

        public MainForm()
        {
            // Configurações básicas da janela
            this.Text = "Tradução TIME";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new System.Drawing.Size(400, 300);

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

            // Adicionar o menu ao MenuStrip
            if (menuStrip != null)
            {
                menuStrip.Items.Add(configMenu);

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

        public void ShowTranslation(string transcription)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowTranslation chamado com: {transcription}");
            Console.WriteLine($"[UI] {transcription}");

            // Thread-safe: garantir que a atualização aconteça na thread da UI
            if (conversationTextBox?.InvokeRequired == true)
            {
                conversationTextBox.Invoke(new Action(() => ShowTranslation(transcription)));
                return;
            }

            if (conversationTextBox != null)
            {
                // Adicionar à transcrição existente (em tempo real)
                conversationTextBox.AppendText(transcription + "\r\n");
                conversationTextBox.ScrollToCaret();
            }
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
            
            // Limpar texto anterior
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
