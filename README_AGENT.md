# 🤖 **AI.EnterpriseRAG - 企业级RAG + Agent解决方案**

## 📋 **项目概述**

这是一个生产级的**企业级RAG（检索增强生成）+ Agent智能体**系统，结合了现代AI技术的最佳实践，提供知识库问答、智能工具调用、任务编排等核心能力。

### **核心特性**

✅ **多租户隔离** - Collection级别向量库隔离 + 数据库租户隔离  
✅ **RBAC权限系统** - 用户/角色/权限三级模型 + JWT动态鉴权  
✅ **RAG检索优化** - Rerank重排 + 相似度过滤 + 混合检索  
✅ **Agent智能体** - ReAct引擎 + 意图识别 + 自动工具调用  
✅ **多向量库支持** - Qdrant + Chroma  
✅ **多LLM支持** - 通义千问 + Ollama  
✅ **流式响应** - SSE支持实时输出  
✅ **AOT优化** - 原生编译支持，启动速度快  

---

##  **架构设计**

```
┌─────────────────────────────────────────────────────────────────┐
│                       WebAPI Layer                               │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────────────────┐│
│  │  Chat API  │  │  Document API │  │  Agent API (NEW)         ││
│  └────────────┘  └──────────────┘  └──────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Application Layer                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │ChatUseCase   │  │DocumentUseCase│  │ Agent Orchestrator   │  │
│  │(RAG Logic)   │  │(Upload+Parse) │  │ (ReAct Engine)       │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Domain Layer                                  │
│  ┌──────────┐  ┌────────────┐  ┌──────────┐  ┌───────────────┐│
│  │Document  │  │ DocumentChunk│  │ChatConv. │  │AgentSession  ││
│  └──────────┘  └────────────┘  └──────────┘  └───────────────┘│
│  ┌──────────────────────────────────────────────────────────────┤
│  │  Interfaces: ILlm, IVectorStore, IToolRegistry, IAgent...   │
│  └──────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                                │
│  ┌────────────┐  ┌────────────┐  ┌──────────────────────────┐ │
│  │Qdrant/Chroma│  │Ollama/Tongyi│  │Tool Registry           │ │
│  │VectorStore  │  │LLM Service  │  │- RagSearchTool         │ │
│  │             │  │             │  │- DataCollectionTool    │ │
│  │             │  │             │  │- LogAnalysisTool       │ │
│  └────────────┘  └────────────┘  └──────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚀 **快速开始**

### **1. 环境准备**

```bash
# .NET 8 SDK
dotnet --version  # 确保 >= 8.0

# MySQL 8.0+
mysql --version

# Qdrant 向量库 (Docker)
docker run -d -p 6333:6333 -v qdrant_storage:/qdrant/storage qdrant/qdrant

# Ollama (本地LLM)
ollama pull qwen2.5:7b
ollama pull nomic-embed-text  # Embedding模型
```

### **2. 配置文件**

编辑 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=enterprise_rag;uid=root;pwd=your_password"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "rag.auth",
    "Audience": "rag.api",
    "ExpireMinutes": 120
  },
  "LlmOptions": {
    "DefaultModel": "ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ModelName": "qwen2.5:7b",
      "EmbeddingModel": "nomic-embed-text"
    }
  },
  "VectorStoreOptions": {
    "DefaultType": "Qdrant",
    "Qdrant": {
      "BaseUrl": "http://localhost:6333",
      "CollectionName": "enterprise_rag_collection",
      "VectorSize": 768,
      "DistanceMetric": "Cosine"
    }
  }
}
```

### **3. 数据库迁移**

```bash
cd AI.EnterpriseRAG.WebAPI
dotnet ef database update --project ../AI.EnterpriseRAG.Infrastructure
```

### **4. 启动服务**

```bash
dotnet run --project AI.EnterpriseRAG.WebAPI
# 访问 https://localhost:7001/swagger
```

---

## 📚 **Agent智能体使用指南**

### **核心能力**

#### **1. 意图识别**
自动识别用户输入的意图类型：
- `RagQuery`: 知识库查询
- `DataAnalysis`: 数据分析
- `Troubleshooting`: 故障诊断/根因分析
- `DataCollection`: 数据采集
- `TaskExecution`: 任务执行

#### **2. 可用工具**

| 工具名称 | 功能描述 | 使用场景 |
|---------|----------|---------|
| `rag_search` | 知识库检索 | "查询产品手册中的XX信息" |
| `data_collection` | 数据采集 | "抓取某个API的最新数据" |
| `log_analysis` | 日志分析 | "分析最近1小时的系统错误日志" |

#### **3. API使用**

##### **流式响应（SSE）**

```bash
curl -X POST "https://localhost:7001/api/agent/execute" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "input": "帮我查询知识库中关于产品安装的文档",
    "maxIterations": 10
  }'
```

响应示例（SSE流）：
```
event: SessionStarted
data: {"session_id":"xxx","timestamp":"2024-01-01T12:00:00Z"}

event: IntentRecognized
data: {"thought":"识别意图: RagQuery (置信度: 0.95)"}

event: Thinking
data: {"step_index":1,"thought":"我需要使用rag_search工具查询知识库"}

event: ToolCalling
data: {"tool_call":{"tool_name":"rag_search","arguments":{"query":"产品安装","top_k":5}}}

event: Observation
data: {"observation":"工具执行成功: {...}"}

event: FinalAnswer
data: {"final_answer":"根据知识库内容，产品安装步骤如下..."}

event: done
data: {"status":"completed"}
```

##### **同步响应（JSON）**

```bash
curl -X POST "https://localhost:7001/api/agent/execute-sync" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "input": "分析最近1小时的系统错误日志",
    "maxIterations": 10
  }'
```

响应：
```json
{
  "sessionId": "xxx-xxx-xxx",
  "finalAnswer": "分析完成。发现3个主要错误类型...",
  "steps": [
    {
      "stepIndex": 1,
      "eventType": "Thinking",
      "thought": "需要使用log_analysis工具",
      "timestamp": "2024-01-01T12:00:00Z"
    },
    {
      "stepIndex": 1,
      "eventType": "ToolCalling",
      "toolName": "log_analysis",
      "toolArguments": {"log_source":"application","time_range":"last_hour"}
    }
  ],
  "totalSteps": 5
}
```

---

## 🔧 **扩展工具开发**

### **创建自定义工具**

```csharp
using AI.EnterpriseRAG.Domain.Interfaces.Agent;

public class WeatherQueryTool : ITool
{
    public string Name => "weather_query";
    public string Description => "查询指定城市的天气信息";
    public string Category => "external";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""city"": {
      ""type"": ""string"",
      ""description"": ""城市名称，如'北京'、'上海'""
    }
  },
  ""required"": [""city""]
}";

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var city = arguments["city"].ToString();
        
        // 调用天气API
        var weatherData = await CallWeatherApiAsync(city);
        
        return ToolResult.Success(
            JsonSerializer.Serialize(weatherData),
            100 // 耗时ms
        );
    }
}
```

### **注册工具**

在 `ToolRegistrationService.cs` 中注册：

```csharp
public Task StartAsync(CancellationToken cancellationToken)
{
    using var scope = _serviceProvider.CreateScope();
    var registry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

    // 注册现有工具
    registry.RegisterTool(scope.ServiceProvider.GetRequiredService<RagSearchTool>());
    registry.RegisterTool(scope.ServiceProvider.GetRequiredService<DataCollectionTool>());
    registry.RegisterTool(scope.ServiceProvider.GetRequiredService<LogAnalysisTool>());
    
    // 注册新工具
    registry.RegisterTool(scope.ServiceProvider.GetRequiredService<WeatherQueryTool>());

    return Task.CompletedTask;
}
```

---

## 📊 **架构优化对比**

### **DocumentChunk实体优化**

#### **优化前（存在问题）**
```csharp
public class DocumentChunk
{
    public string VectorJson { get; set; }      // 冗余：向量已存Qdrant
    public string Embedding { get; set; }       // 冗余：和VectorJson重复
    public string KeyWords { get; set; }        // 未使用
    public float Similarity { get; set; }       // 混淆：临时字段混入持久化
    public string FileName { get; set; }        // 冗余：应从Document获取
    public string FileType { get; set; }        // 冗余
}
```

#### **优化后（清晰分离）**
```csharp
// 持久化实体
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; }
    public int Index { get; set; }
    public string ChunkId { get; set; }
    
    // 非持久化字段（标记为NotMapped）
    public float Similarity { get; set; }  // 仅用于查询传输
    
    public virtual Document Document { get; set; }  // 导航属性
}

// 查询DTO（可选，未来扩展）
public class DocumentChunkSearchResultDto
{
    public DocumentChunk Chunk { get; set; }
    public float Similarity { get; set; }
    public string FileName => Chunk.Document.Name;
}
```

---

## 🎯 **最佳实践建议**

### **1. 生产环境部署**

```yaml
# docker-compose.yml
version: '3.8'
services:
  webapi:
    image: enterprise-rag-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=server=mysql;database=enterprise_rag;uid=root;pwd=prod_password
    ports:
      - "8080:80"
    depends_on:
      - mysql
      - qdrant
      
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: prod_password
      MYSQL_DATABASE: enterprise_rag
    volumes:
      - mysql_data:/var/lib/mysql
      
  qdrant:
    image: qdrant/qdrant
    ports:
      - "6333:6333"
    volumes:
      - qdrant_storage:/qdrant/storage
```

### **2. 性能优化**

- **向量库**: 仅存ID+元数据，内容从DB批量加载
- **缓存策略**: Redis缓存用户权限和会话数据
- **异步处理**: 文档处理使用消息队列（RabbitMQ/Azure Service Bus）
- **连接池**: 配置HTTP客户端连接池复用

### **3. 监控告警**

集成OpenTelemetry + Prometheus：
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

---

## 📖 **示例场景**

### **场景1：知识库智能问答**

**用户输入**: "公司的年假政策是什么？"

**Agent执行链路**:
1. **意图识别** → RagQuery (置信度0.95)
2. **工具调用** → rag_search(query="年假政策", top_k=5)
3. **检索结果** → 找到3篇相关文档
4. **生成答案** → "根据员工手册第X章..."

### **场景2：系统故障诊断**

**用户输入**: "为什么最近1小时系统频繁报错？"

**Agent执行链路**:
1. **意图识别** → Troubleshooting (置信度0.92)
2. **工具调用** → log_analysis(log_source="application", time_range="last_hour")
3. **分析结果** → 发现NullReferenceException高频出现
4. **根因建议** → "检查XX模块的空引用问题，建议..."

### **场景3：数据采集+分析**

**用户输入**: "获取GitHub Trending的最新项目并总结"

**Agent执行链路**:
1. **意图识别** → DataCollection (置信度0.88)
2. **工具调用** → data_collection(source_type="web", url="https://github.com/trending")
3. **数据解析** → 提取前10个项目信息
4. **生成总结** → "本周热门项目包括..."

---

## 🛠 **故障排查**

### **常见问题**

#### **1. 向量库连接失败**
```bash
# 检查Qdrant状态
curl http://localhost:6333/collections

# 重启服务
docker restart qdrant
```

#### **2. LLM调用超时**
```json
// appsettings.json
"LlmOptions": {
  "Ollama": {
    "Timeout": 300  // 增加超时时间到5分钟
  }
}
```

#### **3. Agent无法调用工具**
- 检查工具是否正确注册
- 查看日志确认Prompt格式
- 验证LLM返回的JSON格式

---

## 📝 **后续规划**

- [ ] **Plan-Solve策略** - 多步规划能力
- [ ] **DAG工作流编排** - 类似Langflow的可视化编排
- [ ] **多模态支持** - 图片/表格理解
- [ ] **知识图谱集成** - 实体关系抽取
- [ ] **分布式向量库** - Milvus集群支持
- [ ] **Web管理后台** - Vue3 + Element Plus

---

## 📄 **许可证**

MIT License

---

## 👥 **贡献指南**

欢迎提交PR和Issue！请确保：
1. 代码符合C# 12规范
2. 添加单元测试覆盖
3. 更新相关文档

---

**Built with ❤️ for Enterprise AI Applications**
