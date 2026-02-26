# 🏗️ Diagrama da Arquitetura Refatorada

## Arquitetura Atual (Problemas)

```
┌─────────────────────────────────────────────────────────┐
│                    MainWindow.xaml.cs (525 linhas)      │
│                                                          │
│  ❌ Todas as responsabilidades aqui                    │
│  ❌ Acoplado a implementações concretas                │
│  ❌ Callbacks estáticos                                │
│  ❌ Logging duplicado                                  │
└────────────────┬──────────────────────────────────────┘
                 │
        ┌────────┴────────┬────────────┐
        │                 │            │
        ▼                 ▼            ▼
   Transcrição      Histórico      Config
   (3 classes)      (Singleton)    (Singleton)
        │                │            │
        └────────┬───────┴────────────┘
                 │
        Static Callbacks ❌
        (OnTranscriptionReceivedSegment)
```

## Arquitetura Proposta (Refatorada)

```
┌────────────────────────────────────────────────────────────────┐
│                    DependencyInjection                         │
│                      (Program.cs)                              │
└────────────────────────────────────────────────────────────────┘
         │
         ├─ ILogger ◄──── LoggerService
         ├─ IConfigurationService ◄──── AppConfig
         ├─ IHistoryManager ◄──── HistoryManager
         ├─ ITranscriptionEventPublisher ◄──── TranscriptionEventPublisher
         └─ TranscriptionServiceFactory
                 │
                 ▼
    ┌────────────────────────────────────┐
    │     TranscriptionServiceFactory    │
    │  (Padrão Strategy + Factory)      │
    └────────────────────────────────────┘
         │
    ┌────┴────┬──────────┐
    │          │          │
    ▼          ▼          ▼
 Option1   Option2    Option3
    │          │          │
    ▼          ▼          ▼

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ ITranscription   │  │ ITranscription   │  │ ITranscription   │
│   Service 1      │  │   Service 2      │  │   Service 3      │
│                  │  │                  │  │                  │
│ ✅ Sem Diarização│  │ ✅ Com Diarização│  │  ✅ Captura      │
└────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               │
                               │ Publica eventos
                               ▼
         ┌─────────────────────────────────────┐
         │ ITranscriptionEventPublisher        │
         │                                     │
         │ SegmentReceived ──┐                │
         │ ErrorOccurred  ──┼─┐              │
         │ Started ────────┼┼┐│              │
         │ Completed ─────┼┼┼┼┘              │
         └────────┬────────┴┼┼┼┘              │
                  │         │││              │
                  │         │││ Event-based  │
                  │         │││ (tipado)     │
                  │         │││              │
                  ▼         │││              │
         ┌─────────────────┐│││              │
         │ MainWindow      ││││              │
         │ (Thin UI) ◄─────┘││              │
         │                  ││              │
         │ ✅ 250 linhas    ││              │
         │ ✅ Low coupling  ││              │
         │ ✅ Single Resp   ││              │
         └────────┬─────────┘│              │
                  │          │              │
                  ▼          ▼              │
         MainWindowViewModel  ◄─────────────┘
         (MVVM bindings)
```

## Fluxo de Transcrição - Antes vs Depois

### ANTES ❌ (Acoplamento severo)

```
User clica "Iniciar"
    │
    ▼
MainWindow.ButtonIniciar_Click()
    │
    ├─ Valida config ✓
    │
    ├─ Limpa histórico ✓
    │
    ├─ Switch em string (if opcao == "1")
    │   │
    │   └─ Chama TranscricaoSemDiarizacao.Executar() ❌ (acoplado)
    │       │
    │       └─ TranscricaoSemDiarizacao.OnTranscriptionReceivedSegment = ShowTranslation ❌ (callback estático)
    │           │
    │           └─ ShowTranslation() é chamado
    │               │
    │               ├─ Logger.Info() ✓
    │               ├─ System.Diagnostics.Debug.WriteLine() ✓ (duplicado!)
    │               │
    │               └─ Atualiza UI
    │                   │
    │                   ├─ Dispatcher.Invoke()
    │                   ├─ ViewModel.AddFinalizedLine()
    │                   └─ HistoryManager.Instance.AddMessage() ❌ (singleton)
    │
    └─ PROBLEMA: Se adicionar novo tipo de transcrição, modifica MainWindow ❌
```

### DEPOIS ✅ (Desacoplado com eventos)

```
User clica "Iniciar"
    │
    ▼
MainWindow.ButtonIniciar_Click()
    │
    ├─ Valida config via IConfigurationService ✓
    │
    ├─ Limpa histórico via IHistoryManager ✓
    │
    ├─ Cria serviço via TranscriptionServiceFactory ✓
    │   │
    │   └─ Factory retorna ITranscriptionService
    │       │
    │       └─ Pode ser qualquer implementação (Strategy)
    │
    └─ Chama await service.StartAsync() ✓
        │
        ▼
    Executar Transcrição
        │
        ├─ Usa ITranscriptionEventPublisher.OnSegmentReceived()
        │
        └─ Publica evento (type-safe) ✓
            │
            ▼
    ITranscriptionEventPublisher.SegmentReceived event
        │
        ├─ MainWindow inscrito em eventPublisher.SegmentReceived
        │
        └─ OnSegmentReceived() é chamado (via evento)
            │
            ├─ ILogger.Info() ✓ (centralizado)
            ├─ Dispatcher.Invoke()
            ├─ ViewModel.AddFinalizedLine() ✓
            └─ _historyManager.AddMessage() ✓ (injetado)

BENEFÍCIO: Adicionar novo tipo de transcrição = apenas criar nova class ✅
          MainWindow não muda ✅
          Tudo é typesafe ✅
```

## Comparação de Dependências

### ANTES ❌

```
MainWindow
    ├─ TranscricaoSemDiarizacao (direta)
    ├─ TranscricaoComDiarizacao (direta)
    ├─ CapturaAudio (direta)
    ├─ AppConfig (singleton - direta)
    ├─ HistoryManager (singleton - direta)
    ├─ Logger (singleton - direta)
    └─ MainWindowViewModel
        └─ Mais dependências...

RESULTADO: 6+ diretas, tudo acoplado ❌
```

### DEPOIS ✅

```
MainWindow
    ├─ ITranscriptionEventPublisher (interface)
    ├─ IHistoryManager (interface)
    ├─ IConfigurationService (interface)
    ├─ ILogger (interface)
    ├─ TranscriptionServiceFactory (factory)
    └─ MainWindowViewModel

Todas via Dependency Injection ✓
Fácil de mockar para testes ✓
Fácil de trocar implementações ✓
```

## Estrutura de Pastas

### ANTES ❌

```
TraducaoTIME/
├── UIWPF/
│   ├── MainWindow.xaml.cs (525 linhas, tudo aqui)
│   ├── ConfigWindow.xaml.cs
│   ├── ViewModels/
│   ├── Converters/
│   └── Behaviors/
├── Features/
│   ├── CapturaAudio/
│   ├── TranscricaoComDiarizacao/
│   └── TranscricaoSemDiarizacao/
└── Utils/
    ├── AIService.cs (1108 linhas!)
    ├── Logger.cs
    ├── AppConfig.cs
    └── ...
    
PROBLEMA: Utils é uma "garbage bag" ❌
          Features não seguem interface comum ❌
          MainWindow virou "God Object" ❌
```

### DEPOIS ✅

```
TraducaoTIME/
├── Program.cs (DI Container)
│
├── Core/
│   ├── Abstractions/
│   │   ├── ITranscriptionService.cs
│   │   ├── ITranscriptionEventPublisher.cs
│   │   ├── IHistoryManager.cs
│   │   ├── IConfigurationService.cs
│   │   └── ILogger.cs
│   ├── Events/
│   │   ├── TranscriptionSegmentReceivedEventArgs.cs
│   │   └── TranscriptionErrorEventArgs.cs
│   └── Models/
│       ├── TranscriptionSegment.cs
│       ├── AudioDevice.cs
│       └── HistoryEntry.cs
│
├── Services/
│   ├── Transcription/
│   │   ├── TranscricaoSemDiarizacaoService.cs
│   │   ├── TranscricaoComDiarizacaoService.cs
│   │   ├── CapturaAudioService.cs
│   │   └── TranscriptionServiceFactory.cs
│   ├── History/
│   │   └── HistoryManager.cs
│   ├── Configuration/
│   │   └── AppConfig.cs
│   ├── Logging/
│   │   └── LoggerService.cs
│   ├── Events/
│   │   └── TranscriptionEventPublisher.cs
│   └── AI/
│       ├── AIService.cs
│       └── TranslatorService.cs
│
└── UIWPF/
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs (250 linhas, apenas apresentação)
    ├── ConfigWindow.xaml.cs
    ├── ViewModels/
    │   └── MainWindowViewModel.cs
    ├── Converters/
    └── Behaviors/

✓ Separação clara de responsabilidades
✓ Services isolados por domínio
✓ Core com abstrações
✓ MainWindow enxuto
```

## Padrões de Projeto Utilizados

```
┌──────────────────────────────────────────────────────────┐
│                   Padrões SOLID                          │
├──────────────────────────────────────────────────────────┤
│                                                           │
│ S - Single Responsibility Principle                       │
│     ├─ MainWindow: apenas UI                             │
│     ├─ TranscricaoXXXService: apenas transcrição         │
│     ├─ EventPublisher: apenas publicar eventos           │
│     └─ Factory: apenas criar serviços                    │
│                                                           │
│ O - Open/Closed Principle                                │
│     ├─ Aberto para extensão: novos ITranscriptionService │
│     └─ Fechado para modificação: MainWindow não muda     │
│                                                           │
│ L - Liskov Substitution Principle                        │
│     └─ Qualquer ITranscriptionService é intercambiável   │
│                                                           │
│ I - Interface Segregation Principle                      │
│     ├─ ILogger: apenas logging                           │
│     ├─ IConfigurationService: apenas config              │
│     └─ ITranscriptionService: apenas transcrição         │
│                                                           │
│ D - Dependency Inversion Principle                       │
│     ├─ MainWindow depende de ITranscriptionService       │
│     ├─ Não depende de TranscricaoSemDiarizacao           │
│     └─ Injeção via constructor                           │
│                                                           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                   Design Patterns                        │
├──────────────────────────────────────────────────────────┤
│                                                           │
│ Factory Pattern                                          │
│     └─ TranscriptionServiceFactory                       │
│         Cria ITranscriptionService baseado em option     │
│                                                           │
│ Strategy Pattern                                         │
│     ├─ ITranscriptionService é a estratégia              │
│     ├─ Diferentes implementações (sem/com diarização)    │
│     └─ Escolhidas em runtime                             │
│                                                           │
│ Observer Pattern (Events)                                │
│     ├─ ITranscriptionEventPublisher é o Subject          │
│     ├─ MainWindow é o Observer                           │
│     └─ Aviso de eventos em vez de callbacks              │
│                                                           │
│ Singleton Pattern (com DI)                               │
│     ├─ ILogger registrada como AddSingleton              │
│     ├─ IConfigurationService registrada como Singleton   │
│     └─ Mas agora controlado pelo DI container            │
│                                                           │
│ Dependency Injection                                     │
│     └─ Todas as dependências injetadas via constructor   │
│                                                           │
│ MVVM (Model-View-ViewModel)                              │
│     ├─ MainWindow: View                                  │
│     ├─ MainWindowViewModel: ViewModel                    │
│     └─ Binding via DataContext                           │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

## Sequência de Eventos (Detalhado)

```
1. User Interface
   │
   └─ Click "Iniciar" Button
      │
      └─ MainWindow.ButtonIniciar_Click()

2. Validação
   │
   └─ if (!_configurationService.IsValid()) return;

3. Criação de Serviço
   │
   └─ var service = _transcriptionFactory.CreateService(option);
      │
      └─ Retorna implementação de ITranscriptionService

4. Inicialização de Transcrição
   │
   └─ await service.StartAsync(device, cancellationToken);
      │
      └─ Serviço inicia (em thread de background)

5. Coleta de Áudio & Transcrição
   │
   └─ Serviço processa áudio
      │
      ├─ Recebe dados de Azure
      └─ Processa segmentos

6. Publicação de Eventos
   │
   └─ _eventPublisher.OnSegmentReceived(segment);
      │
      └─ Dispara evento SegmentReceived

7. Inscrição em Eventos
   │
   └─ MainWindow.OnSegmentReceived() é invocado
      │
      ├─ Dispatcher.Invoke() (thread safety)
      │
      ├─ Atualiza ViewModel
      │   └─ MainWindowViewModel.AddFinalizedLine()
      │       └─ ObservableCollection atualiza
      │
      ├─ UI renderiza automaticamente (binding)
      │
      └─ Salva em histórico
          └─ _historyManager.AddMessage()

8. Conclusão
   │
   └─ _eventPublisher.OnTranscriptionCompleted();
      │
      └─ MainWindow.OnTranscriptionCompleted() é invocado
          └─ Atualiza status UI
```

## Benefício: Adicionar Novo Serviço

### Antes (❌ Modifica MainWindow):

```csharp
// Em MainWindow.xaml.cs
private void ButtonIniciar_Click(...)
{
    if (opcao == "1")
        TranscricaoSemDiarizacao.Executar(device);
    else if (opcao == "2")
        TranscricaoComDiarizacao.Executar(device);
    else if (opcao == "3")
        CapturaAudio.Executar(device);
    else if (opcao == "4")  // ← Tem que modificar!
        NovoServiço.Executar(device);
}
```

### Depois (✅ MainWindow não muda):

```csharp
// 1. Criar novo serviço
public class NovoServicoTranscricaoService : ITranscriptionService
{
    public async Task<TranscriptionResult> StartAsync(...)
    { /* implementação */ }
}

// 2. Registrar no DI
services.AddSingleton<NovoServicoTranscricaoService>();

// 3. Adicionar case na factory (AHHH! Tem que modificar um arquivo)
// Mas é APENAS a factory, não MainWindow

// 4. MainWindow continua EXATAMENTE igual ✓
// Não precisa saber de nada
```

BENEFÍCIO: Manutenção centralizada, código cliente protegido! 🛡️

---

## Resumo Visual

```
┌─────────────────────────────────────────────────────────┐
│                 ANTES (Problema)                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  MainWindow                                             │
│      │                                                  │
│      ├─ Sabe de TranscricaoSemDiarizacao ❌             │
│      ├─ Sabe de TranscricaoComDiarizacao ❌             │
│      ├─ Sabe de CapturaAudio ❌                         │
│      ├─ Sabe de AppConfig ❌                            │
│      ├─ Sabe de HistoryManager ❌                       │
│      └─ Sabe de Logger ❌                               │
│                                                          │
│  RESULTADO: Altamente acoplado                          │
│             Difícil de testar                           │
│             Impossível estender sem modificar           │
│                                                          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                 DEPOIS (Solução)                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  MainWindow                                             │
│      │                                                  │
│      ├─ Sabe de ITranscriptionService ✓ (interface)    │
│      ├─ Sabe de IConfigurationService ✓ (interface)    │
│      ├─ Sabe de IHistoryManager ✓ (interface)          │
│      ├─ Sabe de ILogger ✓ (interface)                  │
│      └─ Sabe de ITranscriptionEventPublisher ✓         │
│                                                          │
│  RESULTADO: Baixo acoplamento                           │
│             Fácil de testar (mockar interfaces)         │
│             Fácil de estender (novas implementações)    │
│             Factory padrão para criação                 │
│                                                          │
└─────────────────────────────────────────────────────────┘
```
