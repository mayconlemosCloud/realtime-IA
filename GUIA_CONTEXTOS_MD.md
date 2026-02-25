# 📋 Guia: Sistema de Contextos .MD

## ✅ O que foi implementado?

Você agora pode:

1. **Criar arquivos `.md`** na pasta `/Contextos`
2. **Selecionar qual contexto usar** através de um ComboBox na tela de IA
3. **Usar RAG (Retrieval-Augmented Generation)** para incluir o arquivo .md nas análises

## 🎯 Resposta à sua pergunta: Fine-tuning vs RAG?

**→ RAG é a solução!** ✅

| Aspecto | Fine-tuning | RAG (Implementado) |
|--------|------------|----------|
| Custo | ❌ Caro | ✅ Gratuito |
| Velocidade | ❌ Lento (treina modelo) | ✅ Instantâneo |
| Complexidade | ❌ Muito complexo | ✅ Simples |
| Flexibilidade | ❌ Fixo após treino | ✅ Dinâmico (mude a hora) |
| Memória | ❌ Usa muita RAM | ✅ Eficiente |

---

## 📁 Estrutura de Pastas

```
TraducaoTIME/
├── Contextos/                      ← Pasta criada ✨
│   ├── exemplo-contexto.md         ← Exemplo de uso
│   ├── guia-empresa.md             ← Seus contextos aqui
│   ├── produtos.md
│   └── politicas.md
├── UI/
├── Utils/
└── Features/
```

---

## 🚀 Como Usar

### Passo 1: Criar um arquivo .md
Crie um arquivo na pasta `Contextos/` com qualquer nome:

**Exemplo: `meu-contexto.md`**
```markdown
# Informações da Empresa

## Produtos
- Produto A
- Produto B

## Políticas
- Política 1
- Política 2
```

### Passo 2: Recarregar a lista
1. Abra a tela de IA
2. Clique no botão **"Recarregar"** (ou reinicie a aplicação)
3. Seu arquivo aparecerá no ComboBox

### Passo 3: Selecionar e usar
1. Selecione o arquivo no ComboBox
2. Ative o **checkbox "Ativar RAG"**
3. Faça uma pergunta
4. A IA receberá seu .md como contexto automaticamente! 🎉

---

## 💡 Exemplos de Contextos Úteis

### 1. Contexto de Produto (`produtos.md`)
```markdown
# Nossos Produtos

## Software X
- Versão: 2.0
- Características: ...
- Preço: ...

## Software Y
- Versão: 1.5
- Características: ...
```

### 2. Contexto de Conhecimento (`conhecimento.md`)
```markdown
# Base de Conhecimento

## Termos Técnicos
- Sigla A = ...
- Sigla B = ...

## Procedimentos
1. Passo 1
2. Passo 2
```

### 3. Contexto de Domínio (`traducoes.md`)
```markdown
# Glossário de Tradução

## Financeiro
- Revenue = Receita
- Profit = Lucro

## Técnico
- Bug = Erro
- Feature = Funcionalidade
```

---

## ⚙️ Configuração Técnica

### Como funciona internamente:

```csharp
// Quando você clica em "Perguntar":
1. A sua pergunta é lida
2. O arquivo .md selecionado é carregado
3. O contexto do .md é PREPENDED à pergunta
4. A IA recebe: [CONTEXTO_MD] + [HISTÓRICO_CONVERSA] + [SUA_PERGUNTA]
5. A IA analisa tudo junto e responde com melhor contexto
```

### Localização no código:
- **Arquivo**: `UI/AIForm.cs`
- **Método**: `GetMdContextContent()` - lê o arquivo
- **Método**: `LoadMdFiles()` - lista arquivos disponíveis
- **Método**: `GenerateAIResponse()` - inclui contexto do .md

---

## 🔧 Dicas de Uso

### ✅ Boas Práticas
- Use nomes descritivos: `contexto-vendas.md` ✅
- Mantenha arquivos **pequenos e focados** (< 10KB)
- Atualize contextos conforme necessário
- Use bullet points para melhor leitura pela IA

### ❌ Erros Comuns
- ❌ Colocar arquivo em pasta errada
- ❌ Esquecer de clicar "Recarregar"
- ❌ Usar espaços ou caracteres especiais nos nomes
- ❌ Contexto muito grande (> 100KB)

---

## 🎯 Casos de Uso Real

### Cenário 1: Análise de Atendimento ao Cliente
```
Arquivo: normas-atendimento.md
├─ Políticas de reembolso
├─ Procedimentos padrão
└─ Listas de respostas aprovadas

Pergunta: "Este cliente merece reembolso?"
Resposta: A IA analisa a solicitação usando suas normas! 🎯
```

### Cenário 2: Tradução Especializada
```
Arquivo: glossario-tecnico.md
├─ Termos específicos de domínio
├─ Expressões idiomáticas
└─ Contexto regional

Pergunta: "Como traduzir 'X' neste contexto?"
Resposta: A IA segue seu glossário! 📚
```

### Cenário 3: Análise de Conversa
```
Arquivo: perfil-cliente.md
├─ Histórico do cliente
├─ Preferências
└─ Contexto anterior

Pergunta: "O que o cliente queria dizer?"
Resposta: Análise mais precisa usando perfil! 👤
```

---

## 📊 Comparação: Com vs Sem Contexto

### ❌ SEM Contexto .md
```
User: "Qual foi o tema?"
IA: "Analisando apenas o histórico de áudio..."
```

### ✅ COM Contexto .md
```
User: "Qual foi o tema?"
[IA lê o arquivo .md incluído]
IA: "Baseando-me no contexto fornecido NO ARQUIVO, o tema foi..."
```

---

## 🚨 Troubleshooting

### Problema: Arquivo não aparece no ComboBox
**Solução:**
- Verifique se o arquivo está em `Contextos/`
- Verifique a extensão: deve ser `.md`
- Clique em "Recarregar"
- Reinicie a aplicação

### Problema: Contexto não está sendo usado
**Solução:**
- Certifique-se que o checkbox "Ativar RAG" está marcado ✓
- Selecione um arquivo diferente de "(Nenhum contexto)"
- Verifique o console (Debug) para mensagens de erro

### Problema: IA ignora o contexto
**Solução:**
- O arquivo pode estar muito grande
- Tente usar um arquivo menor primeiro
- Se usar OpenAI, verifique sua API Key
- Formule a pergunta mais claramente

---

## 📚 Próximos Passos Opcionais

Se quiser melhorar ainda mais:

1. **Busca Semântica** - Buscar apenas partes relevantes do .md
2. **Múltiplos Contextos** - Selecionar vários .md ao mesmo tempo
3. **Contexto Dinâmico** - Gerar .md dinamicamente
4. **Versionamento** - Manter histórico de contextos

---

## 📝 Resumo

| Antes | Depois |
|-------|--------|
| IA analisava apenas o áudio | ✨ IA usa áudio + seu contexto |
| Sem flexibilidade | ✨ Trocar contexto com 1 clique |
| Respostas genéricas | ✨ Respostas contextualizadas |
| Sem documentação externa | ✨ Use documentos .md próprios |

---

**Implementação completada!** 🎉

Qualquer dúvida, verifique o arquivo `AIForm.cs` na pasta `UI/`
