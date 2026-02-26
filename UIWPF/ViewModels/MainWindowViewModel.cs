using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TraducaoTIME.UIWPF.Models;

namespace TraducaoTIME.UIWPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _currentInterimText = "";
        private ObservableCollection<FinalizedLineItem> _finalizedLines;

        public string CurrentInterimText
        {
            get => _currentInterimText;
            set
            {
                if (_currentInterimText != value)
                {
                    _currentInterimText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<FinalizedLineItem> FinalizedLines => _finalizedLines;

        public MainWindowViewModel()
        {
            _finalizedLines = new ObservableCollection<FinalizedLineItem>();
        }

        public void AddFinalizedLine(string text, string? speaker = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] AddFinalizedLine: Speaker='{speaker}', Text='{text}'");

                var item = new FinalizedLineItem
                {
                    Text = text,
                    Speaker = speaker ?? "",
                    EnglishSuggestion = "",
                    ShowSuggestion = false,
                    IsLoadingSuggestion = false
                };

                System.Diagnostics.Debug.WriteLine($"[ViewModel] Item criado com DisplayText='{item.DisplayText}'");

                _finalizedLines.Add(item);

                System.Diagnostics.Debug.WriteLine($"[ViewModel] Item adicionado. Total: {_finalizedLines.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] ERRO em AddFinalizedLine: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Stack: {ex.StackTrace}");
            }
        }

        public void ClearAllLines()
        {
            _finalizedLines.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
