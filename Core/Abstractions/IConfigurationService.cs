using System;
using NAudio.CoreAudioApi;

namespace TraducaoTIME.Core.Abstractions
{
    public interface IConfigurationService
    {
        string SelectedOption { get; set; }
        MMDevice? SelectedDevice { get; }
        string? SelectedDeviceName { get; set; }
        event EventHandler? ConfigurationChanged;
        bool IsValid();
    }
}
