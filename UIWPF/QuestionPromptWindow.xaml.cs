using System;
using System.Windows;

namespace TraducaoTIME.UIWPF
{
    public partial class QuestionPromptWindow : Window
    {
        public string? Question { get; private set; } = null;
        public bool WasAnalyzed { get; private set; } = false;

        public QuestionPromptWindow()
        {
            InitializeComponent();
            QuestionTextBox.Focus();
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            string question = QuestionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(question))
            {
                MessageBox.Show("Por favor, digite uma pergunta before prosseguir.", "Pergunta Vazia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Question = question;
            WasAnalyzed = true;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
