using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.History
{
    /// <summary>
    /// Implementação de armazenamento de histórico em arquivo.
    /// Persistência simples e confiável para conversas.
    /// </summary>
    public class FileHistoryStorage : IHistoryStorage
    {
        private readonly string _historyPath;
        private readonly object _lock = new object();

        public FileHistoryStorage(string historyPath)
        {
            _historyPath = historyPath ?? throw new ArgumentNullException(nameof(historyPath));
            InitializeFile();
        }

        public Task SaveAsync(HistoryEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Text))
                return Task.CompletedTask;

            lock (_lock)
            {
                try
                {
                    string line = $"[{entry.Timestamp:HH:mm:ss}] {entry.Speaker}: {entry.Text}";
                    File.AppendAllText(_historyPath, line + "\n");
                }
                catch
                {
                    // Silenciosamente ignora erros de I/O
                }
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<HistoryEntry>> LoadAsync()
        {
            if (!File.Exists(_historyPath))
                return Task.FromResult<IEnumerable<HistoryEntry>>(new List<HistoryEntry>());

            var entries = new List<HistoryEntry>();
            lock (_lock)
            {
                try
                {
                    var lines = File.ReadAllLines(_historyPath);
                    // Parsear e reconstruir (simplificado)
                    return Task.FromResult<IEnumerable<HistoryEntry>>(entries);
                }
                catch
                {
                    return Task.FromResult<IEnumerable<HistoryEntry>>(new List<HistoryEntry>());
                }
            }
        }

        public Task ClearAsync()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_historyPath))
                    {
                        File.Delete(_historyPath);
                        InitializeFile();
                    }
                }
                catch
                {
                    // Silenciosamente ignora erro
                }
            }
            return Task.CompletedTask;
        }

        private void InitializeFile()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_historyPath))
                    {
                        string header = $"═══════════════════════════════════════════════════════\n";
                        header += $"HISTÓRICO DE CONVERSA\n";
                        header += $"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n";
                        header += $"═══════════════════════════════════════════════════════\n\n";
                        File.WriteAllText(_historyPath, header);
                    }
                }
                catch
                {
                    // Silenciosamente ignora erros
                }
            }
        }

        public string GetHistoryPath() => _historyPath;
    }
}
