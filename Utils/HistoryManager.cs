using System;
using System.IO;
using System.Text;

namespace TraducaoTIME.Utils
{
    public class HistoryManager
    {
        private static HistoryManager? _instance;
        private string _historyFilePath;
        private object _fileLock = new object();

        public static HistoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HistoryManager();
                }
                return _instance;
            }
        }

        private HistoryManager()
        {
            // Criar pasta Histórico se não existir
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "TraducaoTIME");
            string historyFolder = Path.Combine(appFolder, "Historico");

            if (!Directory.Exists(historyFolder))
            {
                Directory.CreateDirectory(historyFolder);
            }

            // Criar arquivo de histórico com timestamp
            string fileName = $"conversa_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            _historyFilePath = Path.Combine(historyFolder, fileName);

            // Inicializar arquivo
            InitializeFile();

            Console.WriteLine($"[HistoryManager] Histórico iniciado: {_historyFilePath}");
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
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HistoryManager] Erro ao inicializar arquivo: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Adiciona uma nova mensagem ao histórico em tempo real
        /// </summary>
        public void AddMessage(string speaker, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_fileLock)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_historyFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] {speaker}:");
                        writer.WriteLine(message);
                        writer.WriteLine();
                    }
                    Console.WriteLine($"[HistoryManager] Mensagem adicionada: {speaker}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HistoryManager] Erro ao adicionar mensagem: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Retorna o conteúdo completo do histórico
        /// </summary>
        public string GetFullHistory()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_historyFilePath))
                    {
                        return File.ReadAllText(_historyFilePath, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HistoryManager] Erro ao ler histórico: {ex.Message}");
                }
            }
            return "";
        }

        /// <summary>
        /// Adiciona uma seção de análise/sugestão ao histórico
        /// </summary>
        public void AddAnalysis(string analysis)
        {
            lock (_fileLock)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_historyFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine();
                        writer.WriteLine("═══════════════════════════════════════════════════════");
                        writer.WriteLine($"ANÁLISE IA - {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        writer.WriteLine("═══════════════════════════════════════════════════════");
                        writer.WriteLine(analysis);
                        writer.WriteLine();
                    }
                    Console.WriteLine($"[HistoryManager] Análise adicionada ao histórico");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HistoryManager] Erro ao adicionar análise: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Retorna o caminho do arquivo de histórico
        /// </summary>
        public string GetHistoryFilePath()
        {
            return _historyFilePath;
        }

        /// <summary>
        /// Abre o arquivo de histórico no editor padrão
        /// </summary>
        public void OpenHistoryFile()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = _historyFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HistoryManager] Erro ao abrir arquivo: {ex.Message}");
            }
        }
    }
}
