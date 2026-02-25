using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TraducaoTIME.UIWPF.Models
{
    public class FinalizedLineItem : INotifyPropertyChanged
    {
        private string _text = "";
        private string _englishSuggestion = "";
        private bool _isLoadingSuggestion = false;
        private bool _showSuggestion = false;
        private string _speaker = "";

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Speaker
        {
            get => _speaker;
            set
            {
                if (_speaker != value)
                {
                    _speaker = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EnglishSuggestion
        {
            get => _englishSuggestion;
            set
            {
                if (_englishSuggestion != value)
                {
                    _englishSuggestion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoadingSuggestion
        {
            get => _isLoadingSuggestion;
            set
            {
                if (_isLoadingSuggestion != value)
                {
                    _isLoadingSuggestion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowSuggestion
        {
            get => _showSuggestion;
            set
            {
                if (_showSuggestion != value)
                {
                    _showSuggestion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayText => !string.IsNullOrEmpty(Speaker) ? $"{Speaker}: {Text}" : Text;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
