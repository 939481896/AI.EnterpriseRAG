# 🚀 Prompt Management Quick Reference

## TL;DR (Too Long; Didn't Read)

**Problem**: Prompts scattered across 6+ files, hard to maintain  
**Solution**: Centralized prompt management system  
**Result**: Edit 1 file instead of 6, config-driven, testable, multi-language

---

## 📁 Where Everything Is

```
Core/Prompts/PromptTemplates.cs        ← All default prompts
Core/Configuration/PromptOptions.cs    ← Config classes
Domain/Interfaces/.../IPromptService   ← Service interface
Infrastructure/Services/PromptService  ← Implementation
WebAPI/appsettings.json                ← Runtime config
```

---

## 🎯 Common Tasks

### Task 1: Add New Prompt

**Step 1**: Add to `PromptTemplates.cs`
```csharp
["rag"] = new Dictionary<string, string>
{
    ["new_prompt_key"] = @"Your prompt template here
Variables: {variable1}, {variable2}"
}
```

**Step 2**: Add helper method (optional)
```csharp
public static string GetNewPrompt(string var1, string var2, string language = "zh-CN")
    => GetPrompt("rag", "new_prompt_key", new Dictionary<string, object>
    {
        ["variable1"] = var1,
        ["variable2"] = var2
    }, language);
```

**Step 3**: Use in service
```csharp
var prompt = _promptService.GetCustomPrompt("rag", "new_prompt_key", variables);
```

---

### Task 2: Override Prompt via Config

**appsettings.json**:
```json
{
  "Prompts": {
    "Hyde": {
      "PromptTemplateOverride": "Your custom prompt: {query}"
    }
  }
}
```

**Restart app** → Done! (no rebuild needed)

---

### Task 3: Switch Language

**appsettings.json**:
```json
{
  "Prompts": {
    "DefaultLanguage": "en-US"  // or "zh-CN"
  }
}
```

---

### Task 4: Use in Service

**Inject dependency**:
```csharp
public class MyService
{
    private readonly IPromptService _promptService;
    
    public MyService(IPromptService promptService)
    {
        _promptService = promptService;
    }
    
    public async Task DoSomething()
    {
        var prompt = _promptService.GetHydePrompt("query", maxLength: 500);
        // ... use prompt
    }
}
```

---

## 🔧 Available Prompts

### RAG
- `GetRagSystemPrompt()` - System instructions
- `GetRagUserPrompt(context, question, history)` - User prompt

### HyDE
- `GetHydePrompt(query, maxLength)` - Query rewriting

### Multi-Query
- `GetMultiQueryPrompt(query, count)` - Similar queries

### Self-Reflection
- `GetSelfReflectionPrompt(question, answer, sources)` - Validation

### Agent
- `GetAgentReActPrompt(tools)` - ReAct system prompt
- `GetIntentRecognitionPrompt(userInput)` - Intent classification

### Memory
- `GetSessionTitlePrompt(firstQuestion, maxLength)` - Auto-title

### Custom
- `GetCustomPrompt(category, key, variables)` - Any prompt

---

## ⚙️ Configuration Options

```json
{
  "Prompts": {
    "DefaultLanguage": "zh-CN",
    "Rag": {
      "SystemPromptOverride": null,      // Custom system prompt
      "IncludeHistory": true,            // Include conversation history?
      "MaxContextLength": 5000           // Max context characters
    },
    "Hyde": {
      "PromptTemplateOverride": null,    // Custom HyDE prompt
      "UseDetailedInstructions": true    // Use detailed version?
    },
    "SelfReflection": {
      "PromptTemplateOverride": null,    // Custom validation prompt
      "RequestImprovement": true         // Ask for improved answer?
    },
    "Agent": {
      "ReActPromptOverride": null,       // Custom ReAct prompt
      "IncludeExamples": false           // Include examples?
    }
  }
}
```

---

## 🧪 Testing

### Mock PromptService
```csharp
var mockPromptService = new Mock<IPromptService>();
mockPromptService.Setup(x => x.GetHydePrompt(It.IsAny<string>(), It.IsAny<int>()))
                 .Returns("Mocked prompt");

var service = new MyService(mockPromptService.Object);
```

### Verify Calls
```csharp
mockPromptService.Verify(
    x => x.GetHydePrompt("test query", 500), 
    Times.Once
);
```

---

## 📊 Prompt Categories

| Category | Keys | Use Case |
|----------|------|----------|
| `rag` | `system`, `user_with_context`, `user_with_context_and_history` | RAG prompts |
| `hyde` | `generate_hypothetical_doc`, `generate_hypothetical_doc_detailed` | Query rewriting |
| `multi_query` | `generate_similar_queries`, `decompose_complex_query` | Multi-query |
| `self_reflection` | `validate_answer`, `validate_answer_simple` | Validation |
| `agent` | `react_system`, `react_user`, `intent_recognition` | Agent |
| `memory` | `generate_session_title`, `summarize_history` | Memory |

---

## 🚨 Common Mistakes

### ❌ DON'T: Hard-code prompts
```csharp
var prompt = "Generate answer for: " + query;  // BAD
```

### ✅ DO: Use PromptService
```csharp
var prompt = _promptService.GetHydePrompt(query);  // GOOD
```

---

### ❌ DON'T: Duplicate prompts
```csharp
// Service A
var prompt1 = "System: You are...";

// Service B
var prompt2 = "System: You are...";  // Duplicate!
```

### ✅ DO: Reuse from PromptService
```csharp
// Both services
var prompt = _promptService.GetRagSystemPrompt();
```

---

### ❌ DON'T: Ignore configuration
```csharp
var prompt = PromptTemplates.GetHydePrompt(query);  // Ignores config!
```

### ✅ DO: Use PromptService (respects config)
```csharp
var prompt = _promptService.GetHydePrompt(query);  // Uses config override if set
```

---

## 📚 Documentation

- **PROMPT_MANAGEMENT_SYSTEM.md**: Full architecture guide
- **PROMPT_COMPARISON.md**: Before/after comparison
- **PROMPT_OPTIMIZATION_SUMMARY.md**: Implementation summary

---

## 🎓 Key Concepts

### 1. Variable Replacement
```csharp
var template = "Question: {query}\nAnswer: {answer}";
var variables = new Dictionary<string, object> 
{
    ["query"] = "What is RAG?",
    ["answer"] = "RAG stands for..."
};
var prompt = PromptTemplates.GetPrompt("category", "key", variables);
```

### 2. Override Priority
1. **Custom override** (appsettings.json) - Highest
2. **Behavioral flags** (UseDetailedInstructions, etc.)
3. **Default template** (PromptTemplates.cs) - Lowest

### 3. Multi-Language
```csharp
// Automatic based on DefaultLanguage config
var prompt = _promptService.GetHydePrompt(query);

// Or explicit
var promptCn = PromptTemplates.GetHydePrompt(query, language: "zh-CN");
var promptEn = PromptTemplates.GetHydePrompt(query, language: "en-US");
```

---

## ✅ Benefits

| Benefit | Impact |
|---------|--------|
| **Maintainability** | Edit 1 file instead of 6+ |
| **Flexibility** | Change via config (no rebuild) |
| **Testability** | Mock IPromptService |
| **Versioning** | Track in Git |
| **Localization** | Add languages easily |
| **Production** | Safe config updates |

---

## 🔗 Quick Links

- [PromptTemplates.cs](AI.EnterpriseRAG.Core/Prompts/PromptTemplates.cs)
- [IPromptService.cs](AI.EnterpriseRAG.Domain/Interfaces/Services/IPromptService.cs)
- [PromptService.cs](AI.EnterpriseRAG.Infrastructure/Services/PromptService.cs)
- [appsettings.json](AI.EnterpriseRAG.WebAPI/appsettings.json)

---

## 💡 Pro Tips

1. **Always use IPromptService in services** (for config support)
2. **Use PromptTemplates directly only for utilities** (when config not needed)
3. **Test prompts independently** from LLM calls
4. **Version prompts in Git** (both code and config)
5. **Document prompt changes** in commit messages

---

**Last Updated**: 2025-01-XX  
**Version**: 1.0  
**Status**: ✅ Production Ready
