using System;
using System.Windows.Forms;
using TraducaoTIME.Utils;

namespace TraducaoTIME.UI
{
    public class ConfigForm : Form
    {
        private Label? labelTranscricao;
        private ComboBox? comboBoxTranscricao;

        private Label? labelDispositivo;
        private ComboBox? comboBoxDispositivo;

        private Button? buttonFechar;

        public ConfigForm()
        {
            // Configurações básicas da janela
            this.Text = "Configurações";
            this.Width = 500;
            this.Height = 400;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new System.Drawing.Size(300, 250);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;

            // Criar controles
            CreateControls();
        }

        private void CreateControls()
        {
            // Label e ComboBox para Tipo de Transcrição
            labelTranscricao = new Label();
            labelTranscricao.Text = "Tipo de Transcrição:";
            labelTranscricao.Location = new System.Drawing.Point(20, 20);
            labelTranscricao.AutoSize = true;
            this.Controls.Add(labelTranscricao);

            comboBoxTranscricao = new ComboBox();
            comboBoxTranscricao.Location = new System.Drawing.Point(20, 45);
            comboBoxTranscricao.Width = 440;
            comboBoxTranscricao.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxTranscricao.Items.Add("1 - Transcrição SEM diarização (tempo real)");
            comboBoxTranscricao.Items.Add("2 - Transcrição COM diarização (tempo real)");
            comboBoxTranscricao.Items.Add("3 - Apenas capturar áudio (sem transcrição)");
            comboBoxTranscricao.SelectedIndex = int.Parse(AppConfig.Instance.SelectedOption) - 1;
            comboBoxTranscricao.SelectedIndexChanged += ComboBoxTranscricao_SelectedIndexChanged!;
            this.Controls.Add(comboBoxTranscricao);

            // Label e ComboBox para Dispositivo de Áudio
            labelDispositivo = new Label();
            labelDispositivo.Text = "Dispositivo de Áudio:";
            labelDispositivo.Location = new System.Drawing.Point(20, 90);
            labelDispositivo.AutoSize = true;
            this.Controls.Add(labelDispositivo);

            comboBoxDispositivo = new ComboBox();
            comboBoxDispositivo.Location = new System.Drawing.Point(20, 115);
            comboBoxDispositivo.Width = 440;
            comboBoxDispositivo.DropDownStyle = ComboBoxStyle.DropDownList;
            CarregarDispositivos();
            comboBoxDispositivo.SelectedIndexChanged += ComboBoxDispositivo_SelectedIndexChanged!;
            this.Controls.Add(comboBoxDispositivo);

            // Botão Fechar
            buttonFechar = new Button();
            buttonFechar.Text = "Fechar";
            buttonFechar.Location = new System.Drawing.Point(385, 320);
            buttonFechar.Width = 75;
            buttonFechar.Click += ButtonFechar_Click!;
            this.Controls.Add(buttonFechar);
        }

        private void CarregarDispositivos()
        {
            try
            {
                var dispositivos = AudioDeviceSelector.GetDispositivosDisponiveis();
                if (comboBoxDispositivo == null) return;
                comboBoxDispositivo.Items.Clear();

                int selectedIndex = 0;
                for (int i = 0; i < dispositivos.Count; i++)
                {
                    string displayName = $"{dispositivos[i].FriendlyName} ({dispositivos[i].DataFlow})";
                    comboBoxDispositivo.Items.Add(displayName);

                    if (dispositivos[i].FriendlyName == AppConfig.Instance.SelectedDeviceName)
                    {
                        selectedIndex = i;
                    }
                }

                if (comboBoxDispositivo.Items.Count > 0)
                    comboBoxDispositivo.SelectedIndex = selectedIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dispositivos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboBoxTranscricao_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Armazena na configuração (opção 1, 2 ou 3)
            if (comboBoxTranscricao?.SelectedIndex >= 0)
                AppConfig.Instance.SelectedOption = (comboBoxTranscricao.SelectedIndex + 1).ToString();
        }

        private void ComboBoxDispositivo_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Armazena o nome do dispositivo selecionado
            try
            {
                var dispositivos = AudioDeviceSelector.GetDispositivosDisponiveis();
                if (comboBoxDispositivo != null && comboBoxDispositivo.SelectedIndex >= 0 && comboBoxDispositivo.SelectedIndex < dispositivos.Count)
                {
                    AppConfig.Instance.SelectedDeviceName = dispositivos[comboBoxDispositivo.SelectedIndex].FriendlyName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao selecionar dispositivo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonFechar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

