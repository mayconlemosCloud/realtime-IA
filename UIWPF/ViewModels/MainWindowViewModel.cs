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
            var item = new FinalizedLineItem
            {
                Text = text,
                Speaker = speaker ?? "",
                EnglishSuggestion = "",
                ShowSuggestion = false,
                IsLoadingSuggestion = false
            };

            _finalizedLines.Add(item);
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
