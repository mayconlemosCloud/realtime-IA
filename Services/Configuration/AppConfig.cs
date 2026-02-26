using NAudio.CoreAudioApi;
using System;
using System.Linq;
using TraducaoTIME.Core.Abstractions;
using TraducaoTIME.Services.Logging;
using TraducaoTIME.Utils;

namespace TraducaoTIME.Services.Configuration
{
    public class AppConfig : IConfigurationService
    {
        private static AppConfig? _instance;

        private string _selectedOption = "1";
        private string _selectedDeviceName = "";

        public event EventHandler? ConfigurationChanged;

        public static AppConfig Instance
        {
            get
            {
                _instance ??= new AppConfig();
                return _instance;
            }
        }

        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string? SelectedDeviceName
        {
            get => _selectedDeviceName;
            set
            {
                if (value != null)
                {
                    _selectedDeviceName = value;
                    ConfigurationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public MMDevice? SelectedDevice
        {
            get
            {
                try
                {
                    var devices = AudioDeviceSelector.GetDispositivosDisponiveis();
                    var device = devices.FirstOrDefault(d => d.FriendlyName == _selectedDeviceName);

                    if (device != null)
                        return device;

                    return devices.FirstOrDefault();
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(_selectedDeviceName) && SelectedDevice != null;
        }
    }
}
