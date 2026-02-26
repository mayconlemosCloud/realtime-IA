using System;
using System.Collections.Generic;

namespace TraducaoTIME.Core.Abstractions
{
    public interface IHistoryManager
    {
        void AddMessage(string speaker, string text);
        IEnumerable<HistoryEntry> GetHistory();
        string GetFormattedHistory();
        void Clear();
        int Count { get; }
    }

    public class HistoryEntry
    {
        public string Speaker { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
