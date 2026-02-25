using NAudio.CoreAudioApi;
using System;
using System.Linq;

namespace TraducaoTIME.Utils
{
    public class AppConfig
    {
        private static AppConfig _instance = new AppConfig();

        public event EventHandler? ConfigChanged;

        private string _selectedOption = "1";
        private string _selectedDeviceName = "";

        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                ConfigChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string SelectedDeviceName
        {
            get => _selectedDeviceName;
            set
            {
                _selectedDeviceName = value;
                ConfigChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Método para obter o dispositivo atual (sempre fresco)
        public MMDevice GetSelectedDevice()
        {
            try
            {
                var devices = AudioDeviceSelector.GetDispositivosDisponiveis();
                var device = devices.FirstOrDefault(d => d.FriendlyName == _selectedDeviceName);
                
                if (device != null)
                    return device;
                
                // Se não encontrar, retorna o primeiro disponível
                return devices.FirstOrDefault() ?? throw new InvalidOperationException("Nenhum dispositivo de áudio disponível");
            }
            catch
            {
                throw new InvalidOperationException("Erro ao obter dispositivo de áudio");
            }
        }

        public static AppConfig Instance => _instance;

        private AppConfig() { }
    }
}
