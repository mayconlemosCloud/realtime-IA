using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TraducaoTIME.Core.Abstractions
{
    /// <summary>
    /// Interface para persistência de histórico de conversas.
    /// Abstrai a implementação (Arquivo, Banco de Dados, Cloud, etc).
    /// </summary>
    public interface IHistoryStorage
    {
        Task SaveAsync(HistoryEntry entry);
        Task<IEnumerable<HistoryEntry>> LoadAsync();
        Task ClearAsync();
    }

    public class HistoryEntry
    {
        public string Speaker { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
