using System;
using System.Windows;

namespace TraducaoTIME.UIWPF
{
    public partial class DetailedResponseWindow : Window
    {
        private string _fullContent = "";

        public DetailedResponseWindow(string question, string content)
        {
            InitializeComponent();
            
            QuestionTextBlock.Text = $"❓ Pergunta: {question}";
            ContentTextBlock.Text = content;
            _fullContent = content;

            // Atualizar contadores
            UpdateStats();
        }

        private void UpdateStats()
        {
            var lineCount = _fullContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
            var charCount = _fullContent.Length;

            LineCountTextBlock.Text = lineCount.ToString();
            CharCountTextBlock.Text = charCount.ToString("N0");
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(_fullContent);
                
                // Feedback visual
                var originalContent = StatusTextBlock.Text;
                StatusTextBlock.Text = "✓ Copiado para área de transferência!";
                StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, ev) =>
                {
                    StatusTextBlock.Text = originalContent;
                    StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Erro ao copiar: {ex.Message}";
                StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
