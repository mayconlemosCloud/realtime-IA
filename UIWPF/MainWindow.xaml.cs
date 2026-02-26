using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TraducaoTIME.Utils;
using TraducaoTIME.Features.TranscricaoSemDiarizacao;
using TraducaoTIME.Features.TranscricaoComDiarizacao;
using TraducaoTIME.UIWPF.Models;
using TraducaoTIME.UIWPF.ViewModels;

namespace TraducaoTIME.UIWPF
{
    public partial class MainWindow : Window
    {
        private HistoryManager? _historyManager;
        private MainWindowViewModel _viewModel;
        private string _currentInterimText = "";
        private bool isTranscribing = false;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar ViewModel
            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;

            // Inicializar gerenciador de histórico
            _historyManager = HistoryManager.Instance;

            // Se inscrever no evento de mudança de configurações
            AppConfig.Instance.ConfigChanged += (sender, e) => AtualizarStatus();

            // Atualizar status inicial
            AtualizarStatus();
        }

        public void ShowTranslation(TranscriptionSegment segment)
        {
            try
            {
                Logger.Debug($"[ShowTranslation] Recebido: IsFinal={segment.IsFinal}, Text='{segment.Text}', Speaker='{segment.Speaker}'");
                System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Recebido: IsFinal={segment.IsFinal}, Text='{segment.Text}', Speaker='{segment.Speaker}'");

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (segment.IsFinal)
                        {
                            Logger.Debug($"[ShowTranslation] Processando como FINAL");
                            System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Processando como FINAL");

                            // Linha finalizada
                            if (!string.IsNullOrWhiteSpace(segment.Text))
                            {
                                string speaker = !string.IsNullOrWhiteSpace(segment.Speaker) ? segment.Speaker : "";

                                Logger.Debug($"[ShowTranslation] Adicionando ao ViewModel: Speaker='{speaker}', Text='{segment.Text}'");
                                System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Adicionando ao ViewModel: Speaker='{speaker}', Text='{segment.Text}'");

                                // Adicionar ao ViewModel (ItemsControl)
                                _viewModel.AddFinalizedLine(segment.Text, speaker);

                                Logger.Debug($"[ShowTranslation] Adicionado ao ViewModel com sucesso. Total linhas: {_viewModel.FinalizedLines.Count}");
                                System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Adicionado ao ViewModel com sucesso. Total linhas: {_viewModel.FinalizedLines.Count}");

                                // Salvar no arquivo de histórico
                                if (_historyManager != null)
                                {
                                    string displaySpeaker = !string.IsNullOrWhiteSpace(speaker) ? speaker : "Participante";
                                    Logger.Debug($"[ShowTranslation] Salvando no histórico: {displaySpeaker}");
                                    System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Salvando no histórico: {displaySpeaker}");
                                    _historyManager.AddMessage(displaySpeaker, segment.Text);
                                }
                                else
                                {
                                    Logger.Warning("[ShowTranslation] HistoryManager é null");
                                    System.Diagnostics.Debug.WriteLine("[ShowTranslation] AVISO: HistoryManager é null");
                                }
                            }

                            _currentInterimText = "";
                        }
                        else
                        {
                            Logger.Debug($"[ShowTranslation] Processando como INTERIM");
                            System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Processando como INTERIM");

                            // Interim text
                            _currentInterimText = !string.IsNullOrWhiteSpace(segment.Speaker)
                                ? $"{segment.Speaker}: {segment.Text}"
                                : segment.Text;
                        }

                        UpdateDisplay();
                    }
                    catch (Exception exInner)
                    {
                        Logger.Error($"[ShowTranslation] ERRO NO DISPATCHER: {exInner.GetType().Name}: {exInner.Message}", exInner);
                        System.Diagnostics.Debug.WriteLine($"[ShowTranslation] ERRO NO DISPATCHER: {exInner.GetType().Name}: {exInner.Message}");
                        System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Stack: {exInner.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[ShowTranslation] ERRO GERAL: {ex.GetType().Name}: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"[ShowTranslation] ERRO GERAL: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowTranslation] Stack: {ex.StackTrace}");
            }
        }

        private void UpdateDisplay()
        {
            // Atualizar transcrição em tempo real
            var transcriptionDoc = new System.Windows.Documents.FlowDocument();
            var transcriptionParagraph = new System.Windows.Documents.Paragraph();

            if (string.IsNullOrEmpty(_currentInterimText))
            {
                transcriptionParagraph.Inlines.Add(new System.Windows.Documents.Run("Aguardando transcrição..."));
            }
            else
            {
                // Seta em laranja
                var arrow = new System.Windows.Documents.Run("⟳ ")
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                    FontWeight = System.Windows.FontWeights.Bold,
                    FontSize = 12
                };
                transcriptionParagraph.Inlines.Add(arrow);

                // Texto em ouro itálico
                var text = new System.Windows.Documents.Run(_currentInterimText)
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)),
                    FontStyle = System.Windows.FontStyles.Italic
                };
                transcriptionParagraph.Inlines.Add(text);
            }

            transcriptionDoc.Blocks.Add(transcriptionParagraph);
            transcriptionTextBox.Document = transcriptionDoc;
        }

        private void AtualizarStatus()
        {
            try
            {
                string opcao = AppConfig.Instance.SelectedOption;
                string descricaoOpcao = "";

                switch (opcao)
                {
                    case "1":
                        descricaoOpcao = "Transcrição SEM diarização";
                        break;
                    case "2":
                        descricaoOpcao = "Transcrição COM diarização";
                        break;
                    case "3":
                        descricaoOpcao = "Apenas capturar áudio";
                        break;
                    default:
                        descricaoOpcao = "Nenhuma";
                        break;
                }

                string dispositivo = !string.IsNullOrWhiteSpace(AppConfig.Instance.SelectedDeviceName)
                    ? AppConfig.Instance.SelectedDeviceName
                    : "Não selecionado";

                statusLabel.Text = $"Modo: {descricaoOpcao} | Dispositivo: {dispositivo}";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Erro ao atualizar status: {ex.Message}";
            }
        }

        private void ButtonIniciar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("===== INICIANDO TRANSCRIÇÃO =====");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] === INICIANDO TRANSCRIÇÃO ===");

                if (string.IsNullOrWhiteSpace(AppConfig.Instance.SelectedDeviceName))
                {
                    Logger.Warning("Dispositivo não está selecionado");
                    System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Dispositivo não está selecionado");
                    System.Windows.MessageBox.Show("Dispositivo não selecionado! Configure em CONFIG primeiro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Logger.Info($"Dispositivo selecionado: {AppConfig.Instance.SelectedDeviceName}");
                System.Diagnostics.Debug.WriteLine($"[ButtonIniciar] Dispositivo selecionado: {AppConfig.Instance.SelectedDeviceName}");

                if (isTranscribing)
                {
                    Logger.Warning("Já está transcrevendo");
                    System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Já está transcrevendo");
                    return;
                }

                isTranscribing = true;
                buttonIniciar.IsEnabled = false;
                buttonParar.IsEnabled = true;

                // Limpar histórico anterior
                Logger.Info("Limpando linhas antigas");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Limpando linhas antigas");
                _viewModel.ClearAllLines();
                _currentInterimText = "";
                transcriptionTextBox.Document = new System.Windows.Documents.FlowDocument();

                Logger.Info("Criando task de transcrição");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Criando task de transcrição");

                // Registrar callbacks ANTES de iniciar transcrição
                Logger.Info("Registrando callbacks de transcrição");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Registrando callbacks de transcrição");
                TranscricaoSemDiarizacao.OnTranscriptionReceivedSegment = ShowTranslation;
                TranscricaoComDiarizacao.OnTranscriptionReceivedSegment = ShowTranslation;
                TraducaoTIME.Features.CapturaAudio.CapturaAudio.OnTranscriptionReceivedSegment = ShowTranslation;

                // Executar transcrição em task (com suporte apropriado a async/await)
                Logger.Info("Iniciando Task.Run para transcrição");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] Iniciando Task.Run");

                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        Logger.Info("[TASK INICIADA] Thread ID: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
                        System.Diagnostics.Debug.WriteLine("[TranscriptionTask] Task iniciada em thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);

                        Logger.Info("[TASK] Obtendo opção selecionada...");
                        string opcao = AppConfig.Instance.SelectedOption;
                        Logger.Info($"[TASK] Opção selecionada: '{opcao}'");
                        System.Diagnostics.Debug.WriteLine($"[TranscriptionTask] Opção: {opcao}");

                        Logger.Info("[TASK] Obtendo dispositivo selecionado...");
                        var device = AppConfig.Instance.GetSelectedDevice();
                        Logger.Info($"[TASK] Dispositivo obtido: {device?.FriendlyName ?? "NULL"}");
                        if (device == null)
                        {
                            Logger.Error("[TASK] ERRO: Dispositivo é NULL!");
                            throw new Exception("Dispositivo selecionado é nulo");
                        }
                        System.Diagnostics.Debug.WriteLine($"[TranscriptionTask] Device FriendlyName: {device.FriendlyName}");

                        if (opcao == "1")
                        {
                            Logger.Info("[TASK] Chamando TranscricaoSemDiarizacao.Executar...");
                            System.Diagnostics.Debug.WriteLine("[TranscriptionTask] Executando opcao 1 (SEM diarização)");
                            TranscricaoSemDiarizacao.Executar(device);
                            Logger.Info("[TASK] TranscricaoSemDiarizacao.Executar retornou");
                        }
                        else if (opcao == "2")
                        {
                            Logger.Info("[TASK] Chamando TranscricaoComDiarizacao.Executar...");
                            System.Diagnostics.Debug.WriteLine("[TranscriptionTask] Executando opcao 2 (COM diarização)");
                            await TranscricaoComDiarizacao.Executar(device);
                            Logger.Info("[TASK] TranscricaoComDiarizacao.Executar completada");
                        }
                        else if (opcao == "3")
                        {
                            Logger.Info("[TASK] Chamando CapturaAudio.Executar...");
                            System.Diagnostics.Debug.WriteLine("[TranscriptionTask] Executando opcao 3 (Captura)");
                            TraducaoTIME.Features.CapturaAudio.CapturaAudio.Executar(device);
                            Logger.Info("[TASK] CapturaAudio.Executar retornou");
                        }
                        else
                        {
                            Logger.Error($"[TASK] Opção desconhecida: {opcao}");
                            throw new Exception($"Opção desconhecida: {opcao}");
                        }

                        Logger.Info("[TASK] Transcrição concluída com sucesso");
                        System.Diagnostics.Debug.WriteLine("[TranscriptionTask] Concluído");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[TASK] ❌ EXCEÇÃO CAPTURADA: {ex.GetType().FullName}", ex);
                        Logger.Error($"[TASK] ❌ Mensagem: {ex.Message}");
                        Logger.Error($"[TASK] ❌ Stack Trace:\n{ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Logger.Error($"[TASK] ❌ InnerException: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                            Logger.Error($"[TASK] ❌ InnerException Stack:\n{ex.InnerException.StackTrace}");
                        }

                        System.Diagnostics.Debug.WriteLine($"[TranscriptionTask] ❌ ERRO: {ex.GetType().Name}: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[TranscriptionTask] Stack: {ex.StackTrace}");

                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                System.Windows.MessageBox.Show($"Erro durante transcrição:\n{ex.GetType().Name}\n{ex.Message}\n\nVeja o log para mais detalhes.", "Erro Detalhado", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                        catch (Exception exShow)
                        {
                            Logger.Error($"[TASK] Erro ao mostrar MessageBox: {exShow.Message}", exShow);
                        }
                    }
                    finally
                    {
                        Logger.Info("[TASK] === FINALIZANDO TASK ===");
                        System.Diagnostics.Debug.WriteLine("[TranscriptionTask] === FINALLY ===");
                        isTranscribing = false;
                        Logger.Info("[TASK] Flag isTranscribing = false");

                        try
                        {
                            Logger.Info("[TASK] Invocando Dispatcher para restaurar botões...");
                            Dispatcher.Invoke(() =>
                            {
                                Logger.Info("[TASK] Dentro do Dispatcher - habilitando Iniciar, desabilitando Parar");
                                buttonIniciar.IsEnabled = true;
                                buttonParar.IsEnabled = false;
                                Logger.Info("[TASK] Botões restaurados");
                            });
                        }
                        catch (Exception exDispatch)
                        {
                            Logger.Error($"[TASK] Erro ao invocar Dispatcher: {exDispatch.Message}", exDispatch);
                        }
                        Logger.Info("[TASK] === TASK FINALIZADA ===");
                    }
                });

                Logger.Info("✅ Task de transcrição iniciada com sucesso");
                System.Diagnostics.Debug.WriteLine("[ButtonIniciar] ✅ Task iniciada");
            }
            catch (Exception ex)
            {
                Logger.Error($"ERRO GERAL EM BUTTONICIAR: {ex.GetType().Name}: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"[ButtonIniciar] ERRO GERAL: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ButtonIniciar] Stack: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"Erro ao iniciar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                isTranscribing = false;
                buttonIniciar.IsEnabled = true;
                buttonParar.IsEnabled = false;
            }
        }

        private void ButtonParar_Click(object sender, RoutedEventArgs e)
        {
            isTranscribing = false;
            buttonIniciar.IsEnabled = true;
            buttonParar.IsEnabled = false;

            // Parar as transcrições
            try
            {
                TranscricaoSemDiarizacao.Parar();
                TranscricaoComDiarizacao.Parar();
                TraducaoTIME.Features.CapturaAudio.CapturaAudio.Parar();
            }
            catch { }
        }

        private void ConfigMenu_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new ConfigWindow();
            configWindow.Owner = this;
            configWindow.ShowDialog();

            // Atualizar status após fechar a janela de configuração
            AtualizarStatus();
        }

        private void IAMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir janela de pergunta
                QuestionPromptWindow promptWindow = new QuestionPromptWindow();
                promptWindow.Owner = this;
                
                if (promptWindow.ShowDialog() == true && promptWindow.WasAnalyzed)
                {
                    string question = promptWindow.Question ?? "";
                    
                    // Executar análise em background
                    System.Threading.ThreadPool.QueueUserWorkItem(async (_) =>
                    {
                        try
                        {
                            // Obter estado do checkbox RAG
                            bool useRAG = true;
                            Dispatcher.Invoke(() =>
                            {
                                if (FindName("enableRAGCheckBox") is System.Windows.Controls.CheckBox ragCheckBox)
                                {
                                    useRAG = ragCheckBox.IsChecked ?? true;
                                }
                            });

                            System.Diagnostics.Debug.WriteLine($"[IAMenu] Usando RAG: {useRAG}");

                            // Ler contexto da conversa do arquivo
                            string conversationContext = "";
                            if (_historyManager != null)
                            {
                                conversationContext = _historyManager.GetFullHistory();
                            }

                            // Chamar AIService para análise com RAG
                            var aiService = AIService.Instance;
                            string analysis;

                            if (useRAG)
                            {
                                System.Diagnostics.Debug.WriteLine($"[IAMenu] Analisando COM RAG: {question}");
                                analysis = aiService.AnalyzeConversationWithRAG(question, conversationContext);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[IAMenu] Análise SEM RAG (apenas contexto geral)");
                                // Se RAG está desativado, gera análise apenas com a pergunta
                                analysis = aiService.AnalyzeConversationWithRAG(question, conversationContext);
                            }

                            // Abrir janela detalhada com resultado
                            Dispatcher.Invoke(() =>
                            {
                                DetailedResponseWindow responseWindow = new DetailedResponseWindow(question, analysis);
                                responseWindow.Owner = this;
                                responseWindow.ShowDialog();
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[IAMenu] Erro durante análise: {ex.Message}");
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Erro ao analisar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IAMenu] Erro geral: {ex.Message}");
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateSuggestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is FinalizedLineItem item)
            {
                // Iniciar geração de sugestão de forma assíncrona
                System.Threading.ThreadPool.QueueUserWorkItem(async (_) =>
                {
                    try
                    {
                        // Mostrar loading
                        Dispatcher.Invoke(() =>
                        {
                            item.ShowSuggestion = true;
                            item.IsLoadingSuggestion = true;
                        });

                        // Obter estado do checkbox RAG
                        bool useRAG = true;
                        Dispatcher.Invoke(() =>
                        {
                            if (FindName("enableRAGCheckBox") is System.Windows.Controls.CheckBox ragCheckBox)
                            {
                                useRAG = ragCheckBox.IsChecked ?? true;
                            }
                        });

                        // Ler contexto da conversa do arquivo
                        string conversationContext = "";
                        if (_historyManager != null)
                        {
                            conversationContext = _historyManager.GetFullHistory();
                        }

                        // Chamar AIService para gerar sugestão em inglês
                        var aiService = AIService.Instance;
                        string suggestion;

                        if (useRAG)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Gerando sugestão COM RAG para: {item.DisplayText}");
                            suggestion = await aiService.GetEnglishSuggestionWithRAGAsync(item.DisplayText, conversationContext);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Gerando sugestão SEM RAG para: {item.DisplayText}");
                            suggestion = await aiService.GetEnglishSuggestionWithoutRAGAsync(item.DisplayText);
                        }

                        // Atualizar UI com a sugestão
                        Dispatcher.Invoke(() =>
                        {
                            item.EnglishSuggestion = suggestion;
                            item.IsLoadingSuggestion = false;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.EnglishSuggestion = $"Erro ao gerar sugestão: {ex.Message}";
                            item.IsLoadingSuggestion = false;
                        });
                    }
                });
            }
        }
    }
}
