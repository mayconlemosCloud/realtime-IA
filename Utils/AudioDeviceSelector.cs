using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace TraducaoTIME.Utils
{
    public class AudioDeviceSelector
    {
        public static List<MMDevice> GetDispositivosDisponiveis()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToList();
        }

        public static MMDevice SelecionarDispositivo()
        {
            var devices = GetDispositivosDisponiveis();

            Console.WriteLine("\nDispositivos disponíveis:");
            for (int i = 0; i < devices.Count; i++)
                Console.WriteLine($"{i}: {devices[i].FriendlyName} ({devices[i].DataFlow})");

            Console.Write("Escolha o dispositivo: ");
            int deviceIndex = int.Parse(Console.ReadLine() ?? "0");
            var device = devices[deviceIndex];

            Console.WriteLine($"\n✓ Dispositivo selecionado: {device.FriendlyName}\n");
            return device;
        }
    }
}
