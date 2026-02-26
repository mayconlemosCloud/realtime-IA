using System;
using System.Windows;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services.Configuration;
using TraducaoTIME.Utils;

namespace TraducaoTIME.UIWPF
{
    public partial class ConfigWindow : Window
    {
        private readonly IConfigurationService _configurationService;

        public ConfigWindow()
        {
            InitializeComponent();
            _configurationService = Services.Configuration.AppConfig.Instance;
            LoadConfiguration();
        }

        public ConfigWindow(IConfigurationService configurationService, ILogger logger)
        {
            InitializeComponent();
            _configurationService = configurationService;
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // Carregar opções de transcrição
            ComboBoxTranscricao.Items.Add("1 - Transcrição SEM diarização (tempo real)");
            ComboBoxTranscricao.Items.Add("2 - Transcrição COM diarização (tempo real)");
            ComboBoxTranscricao.Items.Add("3 - Apenas capturar áudio (sem transcrição)");

            int selectedTranscricao = int.Parse(_configurationService.SelectedOption) - 1;
            ComboBoxTranscricao.SelectedIndex = Math.Max(0, selectedTranscricao);
            ComboBoxTranscricao.SelectionChanged += ComboBoxTranscricao_SelectionChanged;

            // Carregar dispositivos de áudio
            CarregarDispositivos();
            ComboBoxDispositivo.SelectionChanged += ComboBoxDispositivo_SelectionChanged;
        }

        private void CarregarDispositivos()
        {
            try
            {
                var dispositivos = AudioDeviceSelector.GetDispositivosDisponiveis();
                ComboBoxDispositivo.Items.Clear();

                int selectedIndex = 0;
                for (int i = 0; i < dispositivos.Count; i++)
                {
                    string displayName = $"{dispositivos[i].FriendlyName} ({dispositivos[i].DataFlow})";
                    ComboBoxDispositivo.Items.Add(displayName);

                    if (dispositivos[i].FriendlyName == _configurationService.SelectedDeviceName)
                    {
                        selectedIndex = i;
                    }
                }

                if (ComboBoxDispositivo.Items.Count > 0)
                    ComboBoxDispositivo.SelectedIndex = selectedIndex;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao carregar dispositivos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBoxTranscricao_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxTranscricao.SelectedIndex >= 0)
            {
                string selectedOption = (ComboBoxTranscricao.SelectedIndex + 1).ToString();
                _configurationService.SelectedOption = selectedOption;
            }
        }

        private void ComboBoxDispositivo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxDispositivo.SelectedIndex >= 0)
            {
                try
                {
                    var dispositivos = AudioDeviceSelector.GetDispositivosDisponiveis();
                    var selectedDevice = dispositivos[ComboBoxDispositivo.SelectedIndex];
                    _configurationService.SelectedDeviceName = selectedDevice.FriendlyName;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Erro ao selecionar dispositivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
