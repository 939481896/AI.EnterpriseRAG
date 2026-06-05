# 🎯 Prompt Management System - Architecture Documentation

## Overview

This document explains the **centralized Prompt Management System** implemented in AI.EnterpriseRAG v1.0.

### Problem Solved

**Before**: Prompts were scattered across multiple service files, leading to:
- ❌ Hard to maintain (need to search through code)
- ❌ No versioning or A/B testing
- ❌ Mixed languages (Chinese/English) in code
- ❌ No reusability (duplicate prompt fragments)
- ❌ Config separation violated (prompts are configuration, not code)
- ❌ Testing difficulty (hard to mock different prompts)

**After**: Centralized, configurable, testable prompt management:
- ✅ All prompts in one place (`PromptTemplates.cs`)
- ✅ Configuration-driven overrides (`appsettings.json`)
- ✅ Multi-language support (zh-CN, en-US)
- ✅ Easy to version and A/B test
- ✅ Reusable prompt fragments
- ✅ Unit testable

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Services Layer                             │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐│
│  │QueryRewriting  │  │SelfReflection  │  │ ConversationMem││
│  │Service         │  │Service         │  │oryService      ││
│  └───────┬────────┘  └───────┬────────┘  └───────┬────────┘│
│          │                   │                    │         │
│          └───────────────────┼────────────────────┘         │
│                              ↓                               │
│                    ┌───────────────────┐                    │
│                    │  IPromptService   │  ← Interface       │
│                    └─────────┬─────────┘                    │
│                              │                               │
│                              ↓                               │
│                    ┌───────────────────┐                    │
│                    │  PromptService    │  ← Implementation  │
│                    └─────────┬─────────┘                    │
│                              │                               │
└──────────────────────────────┼───────────────────────────────┘
                               │
         ┌─────────────────────┴─────────────────────┐
         ↓                                           ↓
┌──────────────────┐                    ┌──────────────────────┐
│ PromptTemplates  │  ← Static Library  │   PromptOptions      │
│ (hardcoded)      │                    │   (appsettings.json) │
└──────────────────┘                    └──────────────────────┘
```

---

## Components

### 1. PromptTemplates.cs (Core Library)

**Location**: `AI.EnterpriseRAG.Core/Prompts/PromptTemplates.cs`

**Purpose**: Static library containing all default prompts with multi-language support.

**Structure**:
```csharp
private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _templates = new()
{
    ["zh-CN"] = new Dictionary<string, Dictionary<string, string>>
    {
        ["rag"] = new Dictionary<string, string>
        {
            ["system"] = "你是一个专业的企业级AI助手...",
            ["user_with_context"] = "### 参考文档\n{context}...",
        },
        ["hyde"] = { ... },
        ["self_reflection"] = { ... },
        ["agent"] = { ... },
    },
    ["en-US"] = { ... }
};
```

**Key Features**:
- ✅ Organized by language → category → key
- ✅ Variable replacement with `{variable_name}` placeholders
- ✅ Helper methods for common prompts
- ✅ Fallback to default language if not found

**Example Usage**:
```csharp
// Direct usage
var prompt = PromptTemplates.GetPrompt("rag", "system", language: "zh-CN");

// With variables
var prompt = PromptTemplates.GetHydePrompt(
    query: "房价下降的原因", 
    maxLength: 500, 
    language: "zh-CN"
);
```

---

### 2. PromptOptions.cs (Configuration)

**Location**: `AI.EnterpriseRAG.Core/Configuration/PromptOptions.cs`

**Purpose**: Strongly-typed configuration class for runtime overrides.

**Configuration Classes**:
```csharp
public class PromptOptions
{
    public string DefaultLanguage { get; set; } = "zh-CN";
    public RagPromptOptions Rag { get; set; } = new();
    public HydePromptOptions Hyde { get; set; } = new();
    public SelfReflectionPromptOptions SelfReflection { get; set; } = new();
    public AgentPromptOptions Agent { get; set; } = new();
}

public class RagPromptOptions
{
    public string? SystemPromptOverride { get; set; }  // Custom prompt
    public bool IncludeHistory { get; set; } = true;
    public int MaxContextLength { get; set; } = 5000;
}
```

**Key Features**:
- ✅ Allow overriding specific prompts via `appsettings.json`
- ✅ Control prompt behavior (e.g., include history, max length)
- ✅ No code changes needed for prompt updates

---

### 3. IPromptService (Interface)

**Location**: `AI.EnterpriseRAG.Domain/Interfaces/Services/IPromptService.cs`

**Purpose**: Service contract for prompt retrieval.

**Key Methods**:
```csharp
public interface IPromptService
{
    string GetRagSystemPrompt();
    string GetRagUserPrompt(string context, string question, string? history = null);
    string GetHydePrompt(string query, int maxLength = 500);
    string GetMultiQueryPrompt(string query, int count = 2);
    string GetSelfReflectionPrompt(string question, string answer, string sources);
    string GetAgentReActPrompt(string tools);
    string GetIntentRecognitionPrompt(string userInput);
    string GetSessionTitlePrompt(string firstQuestion, int maxLength = 50);
    string GetCustomPrompt(string category, string key, Dictionary<string, object>? variables);
}
```

---

### 4. PromptService (Implementation)

**Location**: `AI.EnterpriseRAG.Infrastructure/Services/PromptService.cs`

**Purpose**: Implementation that combines `PromptTemplates` + `PromptOptions`.

**Key Logic**:
```csharp
public string GetHydePrompt(string query, int maxLength = 500)
{
    // 1. Check for custom override in config
    if (!string.IsNullOrEmpty(_options.Hyde.PromptTemplateOverride))
    {
        return _options.Hyde.PromptTemplateOverride
            .Replace("{query}", query)
            .Replace("{max_length}", maxLength.ToString());
    }
    
    // 2. Use default from PromptTemplates
    var key = _options.Hyde.UseDetailedInstructions 
        ? "generate_hypothetical_doc_detailed" 
        : "generate_hypothetical_doc";
    
    return PromptTemplates.GetPrompt("hyde", key, new Dictionary<string, object>
    {
        ["query"] = query,
        ["max_length"] = maxLength
    }, _options.DefaultLanguage);
}
```

**Features**:
- ✅ Configuration-driven behavior
- ✅ Fallback to default prompts
- ✅ Variable replacement
- ✅ Logging for debugging

---

## Configuration Examples

### appsettings.json

```json
{
  "Prompts": {
    "DefaultLanguage": "zh-CN",
    "Rag": {
      "SystemPromptOverride": null,
      "IncludeHistory": true,
      "MaxContextLength": 5000
    },
    "Hyde": {
      "PromptTemplateOverride": null,
      "UseDetailedInstructions": true
    },
    "SelfReflection": {
      "PromptTemplateOverride": null,
      "RequestImprovement": true
    },
    "Agent": {
      "ReActPromptOverride": null,
      "IncludeExamples": false
    }
  }
}
```

### Custom Prompt Override Example

If you want to change the RAG system prompt without modifying code:

```json
{
  "Prompts": {
    "DefaultLanguage": "zh-CN",
    "Rag": {
      "SystemPromptOverride": "你是一个专业的金融AI助手，专注于提供准确的市场分析和投资建议。请遵循以下原则：\n1. 仅基于参考文档回答\n2. 保持客观中立\n3. 风险提示必须明确",
      "IncludeHistory": true,
      "MaxContextLength": 8000
    }
  }
}
```

**Result**: System will use your custom prompt instead of the default.

---

## Usage Examples

### 1. Service Integration (Recommended)

**Before (Hard-coded)**:
```csharp
public class QueryRewritingService
{
    public async Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken ct)
    {
        var prompt = $@"Write a detailed, informative passage that would perfectly answer this question:

Question: {query}

Write a comprehensive answer (2-3 paragraphs):";

        return await _llm.ChatAsync(prompt, ct);
    }
}
```

**After (Centralized)**:
```csharp
public class QueryRewritingService
{
    private readonly IPromptService _promptService;
    
    public QueryRewritingService(IPromptService promptService)
    {
        _promptService = promptService;
    }
    
    public async Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken ct)
    {
        var prompt = _promptService.GetHydePrompt(query, maxLength: 500);
        return await _llm.ChatAsync(prompt, ct);
    }
}
```

**Benefits**:
- ✅ No hard-coded prompts
- ✅ Easy to change via config
- ✅ Multi-language support
- ✅ Unit testable (mock IPromptService)

---

### 2. Direct Usage (Utility Functions)

```csharp
// Get RAG system prompt
var systemPrompt = PromptTemplates.GetRagSystemPrompt(language: "zh-CN");

// Get RAG user prompt with context
var userPrompt = PromptTemplates.GetRagUserPrompt(
    context: "文档内容...",
    question: "问题是什么？",
    history: "用户: 之前的问题\n助手: 之前的回答",
    language: "zh-CN"
);

// Get HyDE prompt
var hydePrompt = PromptTemplates.GetHydePrompt(
    query: "房价下降的原因",
    maxLength: 500,
    language: "zh-CN"
);

// Get custom prompt
var customPrompt = PromptTemplates.GetPrompt(
    category: "rag",
    key: "system",
    variables: null,
    language: "en-US"
);
```

---

## Prompt Categories

### RAG Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `system` | RAG system instructions | None |
| `user_with_context` | User prompt with context only | `{context}`, `{question}` |
| `user_with_context_and_history` | User prompt with context + history | `{context}`, `{question}`, `{history}` |

### HyDE Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `generate_hypothetical_doc` | Simple HyDE prompt | `{query}` |
| `generate_hypothetical_doc_detailed` | Detailed HyDE prompt | `{query}`, `{max_length}` |

### Multi-Query Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `generate_similar_queries` | Generate similar questions | `{query}`, `{count}` |
| `decompose_complex_query` | Break down complex query | `{query}`, `{count}` |

### Self-Reflection Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `validate_answer` | Validate answer with improvement | `{question}`, `{answer}`, `{sources}` |
| `validate_answer_simple` | Simple validation | `{question}`, `{answer}`, `{sources}` |

### Agent Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `react_system` | ReAct system instructions | `{tools}` |
| `react_user` | ReAct user input | `{user_input}` |
| `intent_recognition` | Intent classification | `{user_input}` |

### Memory Prompts

| Key | Description | Variables |
|-----|-------------|-----------|
| `generate_session_title` | Auto-generate session title | `{first_question}`, `{max_length}` |
| `summarize_history` | Summarize conversation history | `{history}` |

---

## Adding New Prompts

### Step 1: Add to PromptTemplates.cs

```csharp
["rag"] = new Dictionary<string, string>
{
    ["system"] = "...",
    ["user_with_context"] = "...",
    
    // 🆕 Add new prompt
    ["user_with_web_search"] = @"### 网络搜索结果
{web_results}

### 参考文档
{context}

### 当前问题
{question}

请结合网络搜索结果和参考文档回答问题。"
}
```

### Step 2: Add Helper Method (Optional)

```csharp
public static string GetRagWebSearchPrompt(
    string webResults,
    string context,
    string question,
    string language = "zh-CN")
    => GetPrompt("rag", "user_with_web_search", new Dictionary<string, object>
    {
        ["web_results"] = webResults,
        ["context"] = context,
        ["question"] = question
    }, language);
```

### Step 3: Add to IPromptService (Optional)

```csharp
public interface IPromptService
{
    // ... existing methods
    
    string GetRagWebSearchPrompt(string webResults, string context, string question);
}
```

### Step 4: Use in Service

```csharp
var prompt = _promptService.GetRagWebSearchPrompt(webResults, context, question);
```

---

## Testing

### Unit Test Example

```csharp
[Test]
public void GetHydePrompt_ReturnsCorrectPrompt()
{
    // Arrange
    var query = "房价下降的原因";
    var expectedSubstring = "请为以下问题生成一个详细的、信息丰富的假想回答";
    
    // Act
    var prompt = PromptTemplates.GetHydePrompt(query, maxLength: 500, language: "zh-CN");
    
    // Assert
    Assert.Contains(expectedSubstring, prompt);
    Assert.Contains(query, prompt);
}

[Test]
public void PromptService_UsesCustomOverride()
{
    // Arrange
    var options = new PromptOptions
    {
        Hyde = new HydePromptOptions
        {
            PromptTemplateOverride = "Custom: {query}"
        }
    };
    var service = new PromptService(Options.Create(options), Mock.Of<ILogger<PromptService>>());
    
    // Act
    var prompt = service.GetHydePrompt("test query", 500);
    
    // Assert
    Assert.Equal("Custom: test query", prompt);
}
```

---

## Migration Guide

### For Existing Services

If you have existing services with hard-coded prompts:

1. **Inject IPromptService**:
   ```csharp
   public MyService(IPromptService promptService, ...)
   {
       _promptService = promptService;
   }
   ```

2. **Replace hard-coded prompts**:
   ```csharp
   // Before
   var prompt = $"Generate answer for: {query}";
   
   // After
   var prompt = _promptService.GetHydePrompt(query);
   ```

3. **Update tests** to mock IPromptService instead of hard-coded strings.

---

## Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Maintainability** | Search through 6+ files | Edit 1 file (`PromptTemplates.cs`) |
| **Configuration** | Rebuild required | Edit `appsettings.json` |
| **Versioning** | Git commit to code | Git commit to config OR A/B test |
| **Multi-language** | Duplicate prompts | Single source, multiple languages |
| **Testing** | Hard to mock prompts | Mock `IPromptService` easily |
| **Reusability** | Copy-paste prompts | Reuse prompt fragments |
| **Deployment** | Code deployment required | Config update only |

---

## Best Practices

### ✅ DO

- Use `IPromptService` in services (dependency injection)
- Put common prompt fragments in `PromptTemplates`
- Use `{variable}` placeholders for dynamic content
- Test prompts independently from LLM calls
- Version prompts in Git (both code and config)
- Document prompt changes in commit messages

### ❌ DON'T

- Hard-code prompts in service methods
- Mix prompt logic with business logic
- Duplicate prompts across services
- Ignore configuration overrides (always check `_options` first)
- Skip testing prompt generation

---

## Future Enhancements

### v1.1 Planned Features

- [ ] **Prompt Versioning**: Track prompt changes over time
  ```json
  "Hyde": {
    "PromptVersion": "v2.1",
    "PromptTemplateOverride": "..."
  }
  ```

- [ ] **A/B Testing**: Test multiple prompt variants
  ```json
  "Hyde": {
    "Variants": [
      { "name": "v1", "weight": 0.5, "prompt": "..." },
      { "name": "v2", "weight": 0.5, "prompt": "..." }
    ]
  }
  ```

- [ ] **Prompt Analytics**: Track which prompts perform best
  ```csharp
  await _promptAnalytics.TrackUsageAsync("hyde", "generate_hypothetical_doc", success: true);
  ```

- [ ] **Database-backed Prompts**: Store prompts in DB for runtime updates
  ```csharp
  var prompt = await _promptRepository.GetPromptAsync("rag", "system", "zh-CN");
  ```

- [ ] **Prompt Validation**: Ensure required variables are present
  ```csharp
  [RequiredVariables("context", "question")]
  public string GetRagUserPrompt(...)
  ```

---

## Conclusion

The centralized Prompt Management System provides:

✅ **Better Maintainability**: All prompts in one place  
✅ **Flexibility**: Configuration-driven overrides  
✅ **Multi-language Support**: Easy localization  
✅ **Testability**: Mock prompts in unit tests  
✅ **Versioning**: Track prompt changes  
✅ **Production-ready**: No rebuild for prompt updates  

This is a **critical architectural improvement** for long-term maintainability of enterprise AI systems.

---

## References

- [Prompt Engineering Best Practices](https://platform.openai.com/docs/guides/prompt-engineering)
- [.NET Options Pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-XX  
**Author**: System Architect
