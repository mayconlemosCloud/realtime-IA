using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraducaoTIME.Core.Abstractions;

namespace TraducaoTIME.Services.History
{
    /// <summary>
    /// Gerenciador centralizado de histórico de conversas.
    /// Mantém histórico em memória e delega persistência para IHistoryStorage.
    /// </summary>
    public class HistoryManager : IHistoryManager
    {
        private readonly List<HistoryEntry> _entries = new();
        private readonly IHistoryStorage _storage;
        private readonly ILogger _logger;

        public int Count => _entries.Count;

        public HistoryManager(IHistoryStorage storage, ILogger logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Fire and forget - não bloqueia a UI
            _ = _storage.SaveAsync(entry);
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
            // Fire and forget
            _ = _storage.ClearAsync();
        }
    }
}
