using System;
using System.Collections.Generic;
using TraducaoTIME.Core.Abstractions;

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
}
