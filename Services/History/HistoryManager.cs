using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.History
{
    public class HistoryManager : IHistoryManager
    {
        private List<HistoryEntry> _entries = new List<HistoryEntry>();
        private string _historyFilePath;
        private readonly object _fileLock = new object();

        public int Count => _entries.Count;

        public HistoryManager()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = Path.Combine(appDataPath, "TraducaoTIME");
                string historyFolder = Path.Combine(appFolder, "Historico");

                if (!Directory.Exists(historyFolder))
                {
                    Directory.CreateDirectory(historyFolder);
                }

                string fileName = $"conversa_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                _historyFilePath = Path.Combine(historyFolder, fileName);

                InitializeFile();
            }
            catch
            {
                _historyFilePath = Path.Combine(Path.GetTempPath(), $"conversa_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            }
        }

        private void InitializeFile()
        {
            lock (_fileLock)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_historyFilePath, false, Encoding.UTF8))
                    {
                        writer.WriteLine($"═══════════════════════════════════════════════════════");
                        writer.WriteLine($"HISTÓRICO DE CONVERSA");
                        writer.WriteLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        writer.WriteLine($"═══════════════════════════════════════════════════════");
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
                catch
                {
                    // Silenciosamente ignora erros
                }
            }
        }

        public void AddMessage(string speaker, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var entry = new HistoryEntry
            {
                Speaker = speaker,
                Text = text,
                Timestamp = DateTime.Now
            };

            _entries.Add(entry);

            // Escrever em arquivo também
            lock (_fileLock)
            {
                try
                {
                    using (var fileStream = new FileStream(_historyFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        writer.WriteLine($"[{entry.Timestamp:HH:mm:ss}] {speaker}:");
                        writer.WriteLine(text);
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
                catch
                {
                    // Silenciosamente ignora erros de I/O
                }
            }
        }

        public IEnumerable<HistoryEntry> GetHistory()
        {
            return _entries.AsReadOnly();
        }

        public string GetFormattedHistory()
        {
            var sb = new StringBuilder();
            foreach (var entry in _entries)
            {
                sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Speaker}:");
                sb.AppendLine(entry.Text);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
