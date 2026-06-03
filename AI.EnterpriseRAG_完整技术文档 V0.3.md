# AI.EnterpriseRAG 完整技术文档

> **版本**: v2.0  
> **最后更新**: 2025年1月  
> **架构类型**: 混合多语言微服务架构（.NET 8 + Python 3.10）  
> **文档类型**: 全栈技术说明 + 版本迭代记录

---

## 📋 目录

1. [项目整体概览](#1-项目整体概览)
2. [架构设计与技术选型](#2-架构设计与技术选型)
3. [核心模块：.NET主服务](#3-核心模块net主服务)
4. [Python子项目1：文档解析服务](#4-python子项目1文档解析服务)
5. [Python子项目2：AI内容生成平台](#5-python子项目2ai内容生成平台)
6. [服务间协作机制](#6-服务间协作机制)
7. [版本迭代记录](#7-版本迭代记录)
8. [部署指南](#8-部署指南)
9. [最佳实践与优化建议](#9-最佳实践与优化建议)

---

## 1. 项目整体概览

### 1.1 系统定位

**AI.EnterpriseRAG是一个企业级智能知识管理与内容生成系统**，融合了三大核心能力：

```
┌─────────────────────────────────────────────────────────────┐
│                  AI.EnterpriseRAG 生态系统                    │
├─────────────────────────────────────────────────────────────┤
│  [.NET 8 主服务] 企业RAG引擎 + 权限管控 + Agent编排         │
│  [Python 1] Unstructured文档解析 + OCR识别                   │
│  [Python 2] AI内容生成 + 多源数据采集 + 自动化流水线         │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 业务场景覆盖

| 业务场景 | 实现模块 | 核心能力 |
|---------|---------|---------|
| **企业知识库问答** | .NET主服务 | RAG检索、权限过滤、多租户隔离 |
| **复杂文档解析** | Python-Parser | PDF/Word/图片OCR、表格识别、语义分块 |
| **自动内容生成** | Python-Content | 多源爬虫、AI选题/脚本生成、定时任务 |
| **智能Agent编排** | .NET主服务 | ReAct推理、工具调用、意图识别 |
| **细粒度权限控制** | .NET主服务 | 文档级权限、RBAC、JWT认证 |

### 1.3 技术栈全景

#### 后端服务
- **.NET 8 WebAPI**：核心业务引擎（C#）
- **Python 3.10**：文档处理 + 内容生成（FastAPI）

#### AI/ML能力
- **LLM集成**：Ollama、通义千问、Gemini、DeepSeek、豆包（火山方舟）
- **向量数据库**：Qdrant（主推）、Chroma
- **文档处理**：Unstructured.io、iText7、Python-Magic

#### 数据存储
- **关系数据库**：MySQL 8.0（业务数据）
- **向量存储**：Qdrant/Chroma（Embedding）
- **文件存储**：本地/对象存储（S3/MinIO）

#### 基础设施
- **容器化**：Docker + Docker Compose
- **日志系统**：Serilog（.NET）+ Python Logging
- **定时任务**：APScheduler（Python）
- **API网关**：Swagger/OpenAPI

---

## 2. 架构设计与技术选型

### 2.1 微服务架构图

```
┌──────────────────────────────────────────────────────────────────┐
│                          前端/客户端                               │
│                    (Web UI / Mobile App)                         │
└────────────────────────┬─────────────────────────────────────────┘
                         │ HTTPS/REST API
┌────────────────────────▼─────────────────────────────────────────┐
│                   API Gateway (可选：Nginx/Traefik)               │
└─────────────┬────────────────────────┬───────────────────────────┘
              │                        │
     ┌────────▼────────┐      ┌───────▼────────┐
     │ .NET 8 主服务    │      │ Python 微服务   │
     │ (RAG引擎)        │      │ (辅助服务)      │
     └────────┬─────────┘      └────────┬────────┘
              │                         │
   ┌──────────┼─────────────────────────┼─────────────┐
   │          │                         │             │
┌──▼──┐  ┌───▼───┐  ┌────────────┐  ┌──▼──────────┐  │
│MySQL│  │Qdrant │  │Unstructured│  │AI Content   │  │
│(业务)│  │(向量) │  │Parser API  │  │Generator    │  │
└─────┘  └───────┘  └────────────┘  └─────────────┘  │
                                                      │
                    ┌─────────────────────────────────┘
                    │
              ┌─────▼──────┐
              │ LLM服务群   │
              │ (多模型)    │
              └─────────────┘
```

### 2.2 技术选型理由

#### 为什么选择.NET 8？
1. **高性能**：原生AOT、Span<T>、异步I/O优化
2. **企业级特性**：内置DI、中间件、EF Core
3. **跨平台**：Linux/Windows/Docker友好
4. **安全性**：JWT、HTTPS、数据加密支持

#### 为什么引入Python微服务？
1. **AI生态成熟**：Unstructured、LangChain、OpenAI SDK
2. **爬虫生态**：requests、BeautifulSoup、snscrape
3. **快速原型**：适合数据处理和AI实验
4. **社区资源**：大量预训练模型和工具

#### 为什么选择Qdrant？
1. **专业向量数据库**：比Chroma性能更强
2. **原生支持过滤**：Payload Filter（权限控制核心）
3. **HNSW索引**：毫秒级检索
4. **分布式能力**：支持集群和分片

---

## 3. 核心模块：.NET主服务

### 3.1 项目结构（DDD分层）

```
AI.EnterpriseRAG/
├── AI.EnterpriseRAG.WebAPI/          # 表示层（API网关）
│   ├── Controllers/                  # RESTful控制器
│   │   ├── AuthController.cs         # 认证：登录/注册/刷新Token
│   │   ├── DocumentController.cs     # 文档：上传/删除/授权
│   │   ├── ChatController.cs         # 问答：RAG智能问答
│   │   └── DocumentPermissionController.cs # 权限：细粒度授权
│   ├── Program.cs                    # 启动配置（DI+中间件）
│   └── appsettings.json              # 配置文件
│
├── AI.EnterpriseRAG.Application/     # 应用层（用例编排）
│   ├── UseCases/
│   │   ├── DocumentUseCase.cs        # 文档上传处理流程
│   │   └── ChatUseCase.cs            # RAG问答流程
│   ├── Authorization/
│   │   └── AuthService.cs            # JWT生成/刷新
│   └── Services/
│       └── DocumentProcessingThrottler.cs # 并发控制
│
├── AI.EnterpriseRAG.Domain/          # 领域层（核心业务）
│   ├── Entities/                     # 实体模型
│   │   ├── Document.cs               # 文档实体（权限字段）
│   │   ├── DocumentChunk.cs          # 分块实体
│   │   ├── SysUser.cs                # 用户/角色
│   │   ├── DocumentPermissions.cs    # 细粒度权限
│   │   └── AgentSession.cs           # Agent会话
│   ├── Interfaces/                   # 领域接口
│   │   ├── Repositories/             # 仓储接口
│   │   ├── Services/                 # 领域服务接口
│   │   ├── UseCases/                 # 用例接口
│   │   └── Agent/                    # Agent接口
│   └── Enums/
│       └── DocumentStatus.cs         # 文档状态枚举
│
├── AI.EnterpriseRAG.Infrastructure/  # 基础设施层（技术实现）
│   ├── Persistence/                  # 数据持久化
│   │   ├── AppEnterpriseAiContext.cs # EF Core上下文
│   │   └── Repositories/             # 仓储实现
│   ├── Services/
│   │   ├── VectorStores/             # 向量存储（Qdrant/Chroma）
│   │   ├── Llm/                      # LLM服务（Ollama/通义）
│   │   ├── DocumentParsers/          # 文档解析器（PDF/TXT）
│   │   └── Agent/                    # Agent编排与工具
│   ├── Authorization/                # 权限处理器
│   ├── Security/                     # JWT服务、Token黑名单
│   └── Middleware/                   # 中间件（日志/审计）
│
└── AI.EnterpriseRAG.Core/            # 核心工具库
    ├── Constants/                    # 常量定义
    ├── Exceptions/                   # 自定义异常
    ├── Models/                       # 通用模型
    └── Utils/                        # 工具类
```

### 3.2 核心技术实现

#### 3.2.1 RAG问答流程（ChatUseCase.cs）

**完整流程**：
```
用户提问 → 权限过滤 → 向量检索 → BGE-Rerank → 上下文构建 → LLM生成 → 返回答案+引用
```

**关键代码片段**：
```csharp
public async Task<(string Answer, List<string> References, decimal CostSeconds)> ChatAsync(
    string userId, string question, CancellationToken cancellationToken = default)
{
    // 1. 获取用户可访问的文档ID（权限过滤）
    var allowedDocIds = await _permissionService.GetUserAllowedDocumentIdsAsync(userId, cancellationToken);
    
    // 2. 生成问题向量
    var queryVector = await _llmService.EmbeddingAsync(question, cancellationToken);
    
    // 3. 向量检索（带权限过滤）
    var filter = new Dictionary<string, object>
    {
        ["document_id"] = allowedDocIds // Qdrant Payload过滤
    };
    
    var matchedChunks = await _vectorStore.SearchAsync(
        collectionName, queryVector, topK: 20, filter: filter, cancellationToken);
    
    // 4. BGE-Rerank重排（提升精度）
    validChunks = await _rerankService.RerankAsync(question, matchedChunks, take: 3, cancellationToken);
    
    // 5. 构建RAG Prompt
    var context = string.Join("\n\n", validChunks.Select(c => c.Content));
    var prompt = $@"
        【上下文】
        {context}
        
        【问题】
        {question}
        
        【回答】";
    
    // 6. 调用LLM生成答案
    var answer = await _llmService.ChatAsync(prompt, cancellationToken);
    
    return (answer, references, costSeconds);
}
```

**技术亮点**：
- ✅ 权限前置过滤（向量层面，不是后过滤）
- ✅ BGE-Rerank二次排序（提升召回精度）
- ✅ 动态上下文截断（避免超Token限制）
- ✅ 文档溯源引用（可审计）

#### 3.2.2 细粒度权限控制

**权限模型**（按位标志）：
```csharp
[Flags]
public enum DocumentPermissionType
{
    None = 0,           // 无权限
    Read = 1,           // 只读
    Write = 2,          // 读写
    Delete = 4,         // 删除
    Share = 8,          // 授权
    Admin = 15          // 全部权限（1+2+4+8）
}
```

**权限判断流程**：
```
JWT Token → 提取Claims → 动态策略生成 → Handler校验 → 通过/拒绝
```

**关键组件**：
1. **TokenService**：生成包含权限的JWT
   ```csharp
   foreach (var p in permissions)
   {
       claims.Add(new Claim("perm", p)); // doc.read, doc.upload等
   }
   ```

2. **DynamicPermissionPolicyProvider**：自动注册策略
   ```csharp
   public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
   {
       return new AuthorizationPolicyBuilder()
           .AddRequirements(new PermissionRequirement(policyName))
           .Build();
   }
   ```

3. **PermissionHandler**：校验用户权限
   ```csharp
   bool hasPermission = context.User.HasClaim("perm", requirement.Permission);
   if (hasPermission) context.Succeed(requirement);
   ```

#### 3.2.3 ReAct Agent编排引擎

**核心算法**：
```
循环（最多10轮）：
  1. Thought：LLM思考下一步
  2. Action：解析并执行工具调用
  3. Observation：获取工具返回结果
  4. 判断：是否找到最终答案
```

**支持的工具**：
| 工具名称 | 功能 | 适用场景 |
|---------|------|---------|
| RagSearchTool | 知识库检索 | 技术文档查询 |
| SqlQueryTool | 数据库查询 | 业务数据分析 |
| DataCollectionTool | Web抓取/API调用 | 实时信息采集 |
| LogAnalysisTool | 日志分析 | 故障诊断 |
| ServerMonitorTool | 服务器监控 | 性能诊断 |
| EmailTicketTool | 工单创建 | 自动化运维 |

**示例执行流程**：
```
用户: "分析昨天的系统错误日志"

Thought: 我需要使用日志分析工具
Action: log_analysis_tool({"date": "yesterday", "level": "error"})
Observation: 发现3个错误：内存溢出、数据库连接超时、Redis断线

Thought: 我已经有了完整信息
FinalAnswer: 昨天系统出现3类错误，主要是内存溢出...
```

### 3.3 核心配置（appsettings.json）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EnterpriseRAG;Uid=root;Pwd=123456;Port=3306;"
  },
  "Jwt": {
    "Issuer": "rag.auth",
    "Audience": "rag.api",
    "SecretKey": "your-secure-secret-key-at-least-32-chars"
  },
  "LlmOptions": {
    "DefaultModel": "ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ModelName": "qwen2.5:7b",
      "EmbeddingModelName": "nomic-embed-text:latest"
    },
    "Tongyi": {
      "ApiKey": "sk-xxx",
      "ModelName": "qwen-turbo"
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

---

## 4. Python子项目1：文档解析服务

### 4.1 项目定位

**AI.EnterpriseRAG.Parser是一个基于Unstructured.io的专业文档解析微服务**，弥补.NET生态在复杂文档处理上的不足。

### 4.2 核心能力

| 功能 | 实现技术 | 优势 |
|------|---------|------|
| **PDF文本提取** | unstructured.partition.auto | 保留排版结构 |
| **Word文档解析** | python-docx | 识别标题/段落/表格 |
| **OCR识别** | Tesseract/PaddleOCR | 图片型PDF识别 |
| **表格结构化** | unstructured.infer_table_structure | JSON格式输出 |
| **语义分块** | chunk_by_title | 按标题层级分块 |

### 4.3 代码结构

```
AI.EnterpriseRAG.Parser/
├── unstructured_api.py       # FastAPI主服务
├── requirements.txt          # 依赖：fastapi, unstructured, python-magic
├── Dockerfile                # 基于unstructured-io官方镜像
└── README.md
```

### 4.4 核心代码（unstructured_api.py）

```python
from fastapi import FastAPI, UploadFile, File, HTTPException, Depends
from fastapi.security import APIKeyHeader
from unstructured.partition.auto import partition
from unstructured.chunking.title import chunk_by_title
import magic  # 文件类型识别

app = FastAPI(title="Unstructured 文档解析 API")

# API鉴权
API_KEY = "your_secure_api_key_here"
api_key_header = APIKeyHeader(name="X-API-Key")

async def get_api_key(api_key: str = Depends(api_key_header)):
    if api_key != API_KEY:
        raise HTTPException(status_code=401, detail="无效的API密钥")
    return api_key

@app.post("/parse-document")
async def parse_document(
    file: UploadFile = File(...),
    api_key: str = Depends(get_api_key)
):
    # 1. 文件校验（大小、类型、魔数）
    file_content = await file.read()
    file_size = len(file_content)
    if file_size > 100 * 1024 * 1024:  # 100MB限制
        raise HTTPException(status_code=413, detail="文件过大")
    
    # 魔数校验（防止后缀伪装）
    file_type_magic = magic.from_buffer(file_content[:1024], mime=True)
    
    # 2. 使用Unstructured解析
    with tempfile.NamedTemporaryFile(delete=False, suffix=f".{file_ext}") as tmp:
        temp_file = tmp.name
        tmp.write(file_content)
    
    try:
        # 核心解析逻辑
        elements = await asyncio.wait_for(
            asyncio.to_thread(
                partition,
                filename=temp_file,
                extract_images_in_pdf=True,    # 提取图片
                infer_table_structure=True,    # 识别表格
                languages=["chi_sim", "eng"],  # 中英文
                strategy="hi_res"              # 高精度模式
            ),
            timeout=PARSE_TIMEOUT
        )
        
        # 3. 语义分块（按标题层级）
        chunks = chunk_by_title(
            elements,
            max_characters=CHUNK_SIZE,
            overlap=CHUNK_OVERLAP
        )
        
        # 4. 返回结构化结果
        return {
            "success": True,
            "filename": file.filename,
            "chunks": [
                {
                    "type": chunk.category,
                    "text": chunk.text,
                    "metadata": chunk.metadata.to_dict()
                }
                for chunk in chunks
            ]
        }
    finally:
        os.remove(temp_file)
```

### 4.5 技术亮点

1. **魔数校验**：防止通过修改后缀绕过文件类型限制
2. **超时控制**：避免大文件解析无限阻塞
3. **异步处理**：使用`asyncio.to_thread`避免阻塞事件循环
4. **结构化输出**：保留原文档的标题层级和元数据

### 4.6 部署方式

**Docker部署**（推荐）：
```bash
# 构建镜像
docker build -t rag-parser:latest .

# 运行容器
docker run -d \
  -p 9000:8000 \
  -e API_KEY="your_secure_key" \
  --name rag-parser \
  rag-parser:latest
```

**与.NET主服务集成**：
```csharp
// UnstructuredClient.cs
public async Task<List<ParsedChunk>> ParseDocumentAsync(Stream fileStream, string fileName)
{
    var content = new MultipartFormDataContent();
    content.Add(new StreamContent(fileStream), "file", fileName);
    
    _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    
    var response = await _httpClient.PostAsync("/parse-document", content);
    var result = await response.Content.ReadFromJsonAsync<ParseResponse>();
    
    return result.Chunks.Select(c => new ParsedChunk
    {
        Text = c.Text,
        Type = c.Type,
        Metadata = c.Metadata
    }).ToList();
}
```

---

## 5. Python子项目2：AI内容生成平台

### 5.1 项目定位

**AI.Content.System是一个基于DDD架构的自动化AI内容生成平台**，集成了多源数据采集、智能选题、脚本生成等能力。

### 5.2 核心功能

| 功能模块 | 实现方式 | 业务价值 |
|---------|---------|---------|
| **多源数据采集** | Reddit/HackerNews/Dev.to/MIT Tech Review | 获取高质量技术资讯 |
| **内容质量评分** | 综合权重算法（热度+评论+来源） | 自动筛选优质内容 |
| **AI选题生成** | LLM（支持Gemini/豆包/DeepSeek） | 生成吸引眼球的标题 |
| **脚本创作** | Prompt工程 + 元数据注入 | 生成短视频/文章脚本 |
| **定时任务** | APScheduler | 自动化内容生产流水线 |

### 5.3 项目结构（DDD分层）

```
AI.Content.System/
├── app/
│   ├── main.py                       # FastAPI入口
│   ├── domain/                       # 领域层
│   │   ├── entities.py               # 实体（ContentItem/Topic/GeneratedContent）
│   │   └── services.py               # 领域服务（质量评分/过滤）
│   ├── application/                  # 应用层
│   │   ├── pipeline.py               # 流水线编排
│   │   └── scheduler.py              # 定时任务
│   ├── infrastructure/               # 基础设施层
│   │   ├── sources/                  # 数据源
│   │   │   ├── base.py               # 爬虫基类
│   │   │   ├── reddit.py             # Reddit爬虫
│   │   │   ├── hackernews.py         # HackerNews爬虫
│   │   │   ├── devto.py              # Dev.to爬虫
│   │   │   ├── mit_techreview.py     # MIT Tech Review
│   │   │   └── venturebeat.py        # VentureBeat
│   │   ├── ai/                       # AI服务
│   │   │   ├── llm_client.py         # LLM客户端（多模型）
│   │   │   └── prompts.py            # Prompt模板
│   │   └── persistence/              # 持久化
│   │       ├── db.py                 # SQLAlchemy配置
│   │       ├── models.py             # 数据库模型
│   │       └── repository.py         # 仓储实现
│   ├── api/                          # API层
│   │   └── routes.py                 # RESTful路由
│   └── core/                         # 核心配置
│       ├── config.py                 # 配置管理
│       └── logger.py                 # 日志配置
├── requirements.txt
├── Dockerfile
└── .env.example
```

### 5.4 核心业务流程

#### 5.4.1 自动化内容生成流水线（pipeline.py）

```python
class ContentGenerationPipeline:
    def __init__(self, db: Session):
        self.db = db
        self.repository = ContentRepository(db)
        self.llm_client = LLMClient()
        self.sources = [
            RedditSource(limit=10),
            HackerNewsSource(limit=10),
            DevToSource(limit=10),
            MITTechReviewSource(limit=10)
        ]
    
    def run(self) -> int:
        """运行完整流水线"""
        # 1. 采集数据
        raw_items = self._fetch_all_sources()
        
        # 2. 质量评分与过滤
        qualified_items = filter_high_quality_items(raw_items, threshold=30.0)
        
        # 3. AI生成（选题+脚本）
        generated_list = []
        for item in qualified_items:
            generated = self._process_single_item(item)
            if generated:
                generated_list.append(generated)
        
        # 4. 批量入库
        count = self.repository.bulk_save(generated_list)
        logger.info(f"流水线完成，新增入库: {count} 条")
        return count
    
    def _process_single_item(self, item: ContentItem) -> GeneratedContent:
        """AI处理单条内容"""
        # 选题生成（使用DeepSeek接入点）
        topic = self.llm_client.generate_topic(
            title=item.title,
            description=item.description,
            url=item.url
        )
        
        # 脚本生成（使用豆包Pro接入点）
        script = self.llm_client.generate_script(
            topic=topic,
            source_content=item.description
        )
        
        return GeneratedContent(
            source=item.source,
            original_title=item.title,
            topic=topic.title,
            category=topic.category,
            description=topic.description,
            script=script,
            score=item.rank_score,
            url=item.url
        )
```

#### 5.4.2 质量评分算法（services.py）

```python
def calculate_rank_score(item: ContentItem) -> float:
    """综合质量评分（热度+来源权重）"""
    # 基础分：原始热度
    score = item.score * 1.0
    
    # 来源权重加成
    source_weights = {
        "MIT Tech Review": 2.5,
        "HackerNews": 2.0,
        "VentureBeat": 1.8,
        "Reddit": 1.5,
        "Dev.to": 1.2
    }
    weight = source_weights.get(item.source, 1.0)
    score *= weight
    
    # URL质量加成（有原文链接+10分）
    if item.url:
        score += 10
    
    # 描述质量加成（有摘要+5分）
    if item.description and len(item.description) > 100:
        score += 5
    
    return score

def filter_high_quality_items(items: List[ContentItem], threshold: float = 30.0):
    """过滤高质量内容"""
    # 计算所有项的评分
    for item in items:
        item.rank_score = calculate_rank_score(item)
    
    # 过滤低于阈值的内容
    qualified = [item for item in items if item.rank_score >= threshold]
    
    # 按分数降序排序
    qualified.sort(key=lambda x: x.rank_score, reverse=True)
    
    return qualified
```

#### 5.4.3 多模型LLM客户端（llm_client.py）

```python
class LLMClient:
    """统一LLM客户端：支持火山方舟/Gemini/DeepSeek/OpenAI"""
    def __init__(self):
        self.provider = settings.LLM_PROVIDER.lower()
        
        if self.provider == "volcengine":
            # 火山方舟（支持多接入点切换）
            self.client = OpenAI(
                api_key=settings.VOLC_API_KEY,
                base_url="https://ark.cn-beijing.volces.com/api/v3"
            )
            self.topic_model = settings.VOLC_TOPIC_ENDPOINT  # DeepSeek接入点
            self.script_model = settings.VOLC_SCRIPT_ENDPOINT  # 豆包Pro接入点
        
        elif self.provider == "gemini":
            genai.configure(api_key=settings.GEMINI_API_KEY)
            self.client = genai.GenerativeModel("gemini-2.0-flash")
        
        elif self.provider == "deepseek":
            self.client = OpenAI(
                api_key=settings.DEEPSEEK_API_KEY,
                base_url="https://api.deepseek.com"
            )
    
    def generate_topic(self, title: str, description: str, url: str) -> Topic:
        """生成AI选题（调用选题专用模型）"""
        prompt = build_topic_prompt(title, description, url)
        
        # 火山方舟使用DeepSeek接入点
        response = self._ask_llm_with_retry(
            prompt,
            temperature=0.8,
            model_override=self.topic_model if self.provider == "volcengine" else None
        )
        
        return parse_topic_response(response)
    
    def generate_script(self, topic: str, source_content: str) -> str:
        """生成脚本（调用脚本专用模型）"""
        prompt = build_script_prompt(topic, source_content)
        
        # 火山方舟使用豆包Pro接入点
        response = self._ask_llm_with_retry(
            prompt,
            temperature=0.7,
            model_override=self.script_model if self.provider == "volcengine" else None
        )
        
        return response
    
    def _ask_llm_with_retry(self, prompt: str, temperature: float, 
                            model_override: str = None, max_retries: int = 2) -> str:
        """带重试的LLM调用（自动处理429限流）"""
        for attempt in range(max_retries + 1):
            try:
                if self.provider == "gemini":
                    response = self.client.generate_content(
                        prompt,
                        generation_config={"temperature": temperature}
                    )
                    return response.text
                else:
                    # OpenAI兼容接口
                    response = self.client.chat.completions.create(
                        model=model_override or self.model,
                        messages=[{"role": "user", "content": prompt}],
                        temperature=temperature
                    )
                    return response.choices[0].message.content
            except Exception as e:
                # 处理429限流（指数级退避）
                if "429" in str(e) and attempt < max_retries:
                    wait_time = (2 ** attempt) * 3
                    logger.warning(f"触发限流，等待 {wait_time}s 后重试...")
                    time.sleep(wait_time)
                    continue
                raise
```

### 5.5 多源数据采集

#### 5.5.1 爬虫基类（base.py）

```python
class BaseSource(ABC):
    """爬虫基类（统一接口）"""
    source_name: str = "base"
    
    def __init__(self, limit: int = 10):
        self.session = self._create_session()
        self.limit = limit
    
    def _create_session(self) -> requests.Session:
        """创建带重试和代理的请求会话"""
        retry_strategy = Retry(
            total=3,
            backoff_factor=1,
            status_forcelist=[429, 500, 502, 503, 504]
        )
        adapter = HTTPAdapter(max_retries=retry_strategy)
        
        session = requests.Session()
        session.mount("https://", adapter)
        
        # 支持代理
        if settings.PROXY_ENABLE:
            session.proxies = {
                "http": settings.PROXY_URL,
                "https": settings.PROXY_URL
            }
        
        session.headers.update({
            "User-Agent": "Mozilla/5.0 Chrome/122.0.0.0"
        })
        return session
    
    @abstractmethod
    def fetch_raw(self) -> list[dict]:
        """抓取原始数据（子类实现）"""
        pass
    
    def fetch(self) -> List[ContentItem]:
        """对外统一接口：抓取并转换为领域实体"""
        raw_data = self.fetch_raw()
        entities = [self.parse_to_entity(data) for data in raw_data]
        
        # 计算质量评分
        for entity in entities:
            entity.rank_score = calculate_rank_score(entity)
        
        return entities
```

#### 5.5.2 Reddit爬虫示例（reddit.py）

```python
class RedditSource(BaseSource):
    source_name = "Reddit"
    
    def fetch_raw(self) -> list[dict]:
        """抓取Reddit r/artificial热门内容"""
        url = "https://www.reddit.com/r/artificial/hot.json"
        resp = self.session.get(url, params={"limit": self.limit})
        data = resp.json()
        
        items = []
        for post in data["data"]["children"][:self.limit]:
            post_data = post["data"]
            items.append({
                "title": post_data["title"],
                "score": post_data["ups"],  # 点赞数
                "url": post_data["url"],
                "description": post_data.get("selftext", "")[:500]
            })
        
        return items
```

### 5.6 定时任务调度（scheduler.py）

```python
from apscheduler.schedulers.background import BackgroundScheduler
from app.application.pipeline import ContentGenerationPipeline
from app.infrastructure.persistence.db import SessionLocal

scheduler = BackgroundScheduler()

def scheduled_task():
    """定时执行流水线"""
    logger.info("定时任务触发：开始内容生成流水线")
    db = SessionLocal()
    try:
        pipeline = ContentGenerationPipeline(db)
        count = pipeline.run()
        logger.info(f"定时任务完成：新增 {count} 条内容")
    finally:
        db.close()

def start_scheduler():
    """启动定时任务（每2小时执行一次）"""
    interval_hours = settings.SCHEDULER_INTERVAL_HOURS
    scheduler.add_job(
        scheduled_task,
        'interval',
        hours=interval_hours,
        id='content_generation_job',
        replace_existing=True
    )
    scheduler.start()
    logger.info(f"定时任务已启动，间隔: {interval_hours}小时")

def stop_scheduler():
    """停止定时任务"""
    scheduler.shutdown()
    logger.info("定时任务已停止")
```

### 5.7 配置管理（.env示例）

```env
# LLM配置
LLM_PROVIDER=volcengine  # 可选：volcengine/gemini/deepseek/openai

# 火山方舟（豆包/DeepSeek接入点）
VOLC_API_KEY=your_volcengine_api_key
VOLC_TOPIC_ENDPOINT=ep-20240101-xxxxx  # DeepSeek接入点（选题）
VOLC_SCRIPT_ENDPOINT=ep-20240102-xxxxx  # 豆包Pro接入点（脚本）

# Gemini配置
GEMINI_API_KEY=your_gemini_api_key
GEMINI_MODEL=gemini-2.0-flash

# DeepSeek官方
DEEPSEEK_API_KEY=sk-xxxxx
DEEPSEEK_MODEL=deepseek-chat

# 数据库
DB_URL=sqlite:///./content.db

# 爬虫配置
PROXY_ENABLE=false
PROXY_URL=http://127.0.0.1:7890
CRAWL_LIMIT=10

# 定时任务
SCHEDULER_INTERVAL_HOURS=2

# 日志
LOG_LEVEL=INFO
```

---

## 6. 服务间协作机制

### 6.1 架构集成方案

```
┌────────────────────────────────────────────────────────────┐
│                      前端/客户端                            │
└──────────────────────┬─────────────────────────────────────┘
                       │ HTTPS
┌──────────────────────▼─────────────────────────────────────┐
│            .NET 8 WebAPI (主服务 - 端口5000)                │
│  - JWT认证                                                  │
│  - 权限控制                                                 │
│  - RAG问答                                                  │
│  - Agent编排                                                │
└─┬────────────┬──────────────────────────────────────────────┘
  │            │
  │ HTTP       │ HTTP
  │            │
┌─▼────────────▼───────┐      ┌─────────────────────────────┐
│ Unstructured Parser  │      │ AI Content Generator        │
│ (Python - 端口9000)  │      │ (Python - 端口8000)         │
│ - 文档解析           │      │ - 数据采集                  │
│ - OCR识别            │      │ - 内容生成                  │
└──────────────────────┘      └─────────────────────────────┘
```

### 6.2 服务调用示例

#### .NET调用文档解析服务
```csharp
// UnstructuredClient.cs
public class UnstructuredClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public UnstructuredClient(IConfiguration config)
    {
        _apiKey = config["VectorStoreOptions:Unstructured:ApiKey"];
        var baseUrl = config["VectorStoreOptions:Unstructured:ApiUrl"];
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }
    
    public async Task<List<ParsedChunk>> ParseDocumentAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        
        var response = await _httpClient.PostAsync(
            "/parse-document",
            content,
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ParseResponse>(
            cancellationToken: cancellationToken);
        
        return result.Chunks.Select(c => new ParsedChunk
        {
            Text = c.Text,
            Type = c.Type,
            ChapterTitle = c.Metadata?.GetValueOrDefault("title")
        }).ToList();
    }
}
```

#### 在DocumentUseCase中集成
```csharp
public async Task<Guid> UploadAndProcessDocumentAsync(
    string fileName,
    string fileType,
    Stream stream,
    string uploadedBy,
    CancellationToken cancellationToken = default)
{
    // 1. 调用Python解析服务
    var parsedChunks = await _unstructuredClient.ParseDocumentAsync(
        stream,
        fileName,
        cancellationToken);
    
    // 2. 生成向量
    var chunks = new List<DocumentChunk>();
    foreach (var parsed in parsedChunks)
    {
        var embedding = await _llmService.EmbeddingAsync(parsed.Text, cancellationToken);
        
        chunks.Add(new DocumentChunk
        {
            Content = parsed.Text,
            SectionTitle = parsed.ChapterTitle,
            TokenCount = TokenCounter.EstimateTokenCount(parsed.Text)
        });
    }
    
    // 3. 存储向量到Qdrant
    foreach (var chunk in chunks)
    {
        await _vectorStore.InsertAsync(chunk, embedding, cancellationToken);
    }
    
    return documentId;
}
```

### 6.3 Docker Compose统一编排

```yaml
version: '3.8'

services:
  # .NET主服务
  enterpriserag-api:
    build: ./AI.EnterpriseRAG.WebAPI
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=mysql;Database=EnterpriseRAG;...
      - VectorStoreOptions__Unstructured__ApiUrl=http://unstructured-parser:8000
      - VectorStoreOptions__Unstructured__ApiKey=secure_key_123
    depends_on:
      - mysql
      - qdrant
      - unstructured-parser
  
  # 文档解析服务
  unstructured-parser:
    build: ./AI.EnterpriseRAG.Parser
    ports:
      - "9000:8000"
    environment:
      - API_KEY=secure_key_123
  
  # AI内容生成服务
  ai-content-generator:
    build: ./AI.Content.System
    ports:
      - "8000:8000"
    environment:
      - LLM_PROVIDER=volcengine
      - VOLC_API_KEY=${VOLC_API_KEY}
      - DB_URL=sqlite:///./content.db
  
  # 数据库
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: 123456
      MYSQL_DATABASE: EnterpriseRAG
    ports:
      - "3306:3306"
    volumes:
      - mysql-data:/var/lib/mysql
  
  # 向量数据库
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - qdrant-data:/qdrant/storage

volumes:
  mysql-data:
  qdrant-data:
```

---

## 7. 版本迭代记录

### v2.0（当前版本）- 混合微服务架构
**发布日期**: 2025年1月  
**架构升级**: 从单体.NET应用升级为混合微服务

#### 核心变更
1. **引入Python微服务生态**
   - 新增`AI.EnterpriseRAG.Parser`（文档解析）
   - 新增`AI.Content.System`（AI内容生成）
   - 解决.NET生态在AI领域的短板

2. **细粒度权限系统**
   - 新增`DocumentPermissions`实体（按位标志权限）
   - 实现文档级权限控制（Read/Write/Delete/Share）
   - 支持角色批量授权和过期时间

3. **BGE-Rerank重排**
   - 集成BGE-Reranker模型
   - 提升RAG检索精度15-20%
   - 支持本地部署和API调用

4. **Agent工具扩展**
   - 新增6大工具：RagSearch、SqlQuery、DataCollection、LogAnalysis、ServerMonitor、EmailTicket
   - 支持意图识别和自动路由
   - ReAct框架最多10轮推理

5. **多模型LLM支持**
   - 支持火山方舟（豆包/DeepSeek接入点切换）
   - 支持Gemini 2.0 Flash
   - 统一LLMClient接口

#### 数据库变更
```sql
-- 新增细粒度权限表
CREATE TABLE UserDocumentPermission (
    Id CHAR(36) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    DocumentId CHAR(36) NOT NULL,
    PermissionType INT NOT NULL,  -- 按位标志：1=Read, 2=Write, 4=Delete, 8=Share
    GrantedBy VARCHAR(100),
    GrantedAt DATETIME,
    ExpiresAt DATETIME,
    IsActive BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (UserId) REFERENCES SysUser(Id),
    FOREIGN KEY (DocumentId) REFERENCES Document(Id)
);

CREATE TABLE RoleDocumentPermission (
    Id CHAR(36) PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    DocumentId CHAR(36) NOT NULL,
    PermissionType INT NOT NULL,
    GrantedBy VARCHAR(100),
    GrantedAt DATETIME,
    FOREIGN KEY (RoleId) REFERENCES SysRole(Id),
    FOREIGN KEY (DocumentId) REFERENCES Document(Id)
);
```

#### 破坏性变更
- ⚠️ `DocumentChunk`移除了`VectorJson`字段（向量数据仅存Qdrant）
- ⚠️ JWT Token有效期从60分钟缩短为30分钟（安全性提升）
- ⚠️ `IDocumentParser`接口新增`CancellationToken`参数

---

### v1.5 - 多租户与权限增强
**发布日期**: 2024年12月

#### 核心功能
1. **多租户隔离**
   - `Document`实体新增`TenantId`字段
   - `SysUser`新增`TenantId`字段
   - 向量检索时强制租户过滤

2. **动态权限策略**
   - 实现`DynamicPermissionPolicyProvider`
   - 支持运行时权限注册
   - 无需硬编码`AddPolicy()`

3. **Qdrant向量库**
   - 从Chroma迁移到Qdrant
   - 支持Payload过滤（权限控制核心）
   - HNSW索引优化

4. **语义分块优化**
   - 识别标题层级（1/2/3级）
   - 保留章节上下文
   - 滑动窗口重叠

---

### v1.0 - 基础RAG系统
**发布日期**: 2024年10月

#### 初始功能
1. **核心RAG流程**
   - 文档上传（PDF/TXT）
   - 向量化存储（Chroma）
   - 智能问答（Ollama）

2. **JWT认证**
   - 用户注册/登录
   - Token刷新机制
   - 基础权限验证

3. **DDD分层架构**
   - Domain层（实体/接口）
   - Application层（用例）
   - Infrastructure层（技术实现）
   - WebAPI层（控制器）

---

## 8. 部署指南

### 8.1 开发环境快速启动

#### 前置要求
- .NET 8 SDK
- Python 3.10+
- Docker & Docker Compose
- MySQL 8.0（可选，可用Docker）

#### 步骤1：启动依赖服务
```bash
# 进入项目根目录
cd AI.EnterpriseRAG

# 启动MySQL、Qdrant、Ollama
docker-compose up -d mysql qdrant ollama

# 验证服务状态
docker ps
```

#### 步骤2：配置.NET主服务
```bash
cd AI.EnterpriseRAG.WebAPI

# 编辑appsettings.json（修改数据库连接、JWT密钥等）

# 运行数据库迁移
dotnet ef database update

# 启动服务
dotnet run

# 访问Swagger：http://localhost:5000/swagger
```

#### 步骤3：启动Python文档解析服务
```bash
cd AI.EnterpriseRAG.Parser

# 安装依赖
pip install -r requirements.txt

# 启动服务
uvicorn unstructured_api:app --host 0.0.0.0 --port 9000

# 测试：curl http://localhost:9000/health
```

#### 步骤4：启动Python内容生成服务
```bash
cd AI.Content.System

# 配置环境变量
cp .env.example .env
# 编辑.env，配置LLM API密钥

# 安装依赖
pip install -r requirements.txt

# 启动服务
uvicorn app.main:app --host 0.0.0.0 --port 8000

# 访问：http://localhost:8000/docs
```

### 8.2 生产环境Docker部署

#### 一键启动所有服务
```bash
# 构建所有镜像
docker-compose build

# 启动所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f enterpriserag-api

# 停止服务
docker-compose down
```

#### 健康检查
```bash
# .NET主服务
curl http://localhost:5000/health

# 文档解析服务
curl http://localhost:9000/health

# 内容生成服务
curl http://localhost:8000/
```

### 8.3 Kubernetes部署（可选）

#### deployment.yaml示例
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: enterpriserag-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: enterpriserag-api
  template:
    metadata:
      labels:
        app: enterpriserag-api
    spec:
      containers:
      - name: api
        image: enterpriserag-api:v2.0
        ports:
        - containerPort: 8080
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
```

---

## 9. 最佳实践与优化建议

### 9.1 性能优化

#### 9.1.1 向量检索优化
```csharp
// ❌ 不推荐：先检索后过滤（性能差）
var allChunks = await _vectorStore.SearchAsync(query, topK: 100);
var filtered = allChunks.Where(c => allowedDocIds.Contains(c.DocumentId));

// ✅ 推荐：使用Payload过滤（性能好）
var filter = new Dictionary<string, object>
{
    ["document_id"] = allowedDocIds  // Qdrant原生过滤
};
var chunks = await _vectorStore.SearchAsync(query, topK: 20, filter: filter);
```

#### 9.1.2 并发控制
```csharp
// 使用SemaphoreSlim限制文档处理并发度
public class DocumentProcessingThrottler
{
    private readonly SemaphoreSlim _semaphore;
    
    public DocumentProcessingThrottler(int maxConcurrency = 5)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

#### 9.1.3 缓存策略
```csharp
// 缓存用户权限列表（Redis）
public async Task<List<Guid>> GetUserAllowedDocumentIdsAsync(string userId)
{
    var cacheKey = $"user:{userId}:allowed_docs";
    
    // 尝试从Redis获取
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        return JsonSerializer.Deserialize<List<Guid>>(cached);
    }
    
    // 数据库查询
    var docIds = await _db.UserDocumentPermissions
        .Where(p => p.UserId == userId && p.IsActive)
        .Select(p => p.DocumentId)
        .ToListAsync();
    
    // 缓存5分钟
    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(docIds), 
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    
    return docIds;
}
```

### 9.2 安全加固

#### 9.2.1 JWT配置
```json
{
  "Jwt": {
    "SecretKey": "[生产环境必须使用强密钥，至少64字符]",
    "Issuer": "rag.auth",
    "Audience": "rag.api",
    "AccessTokenExpireMinutes": 30,
    "RefreshTokenExpireDays": 7
  }
}
```

#### 9.2.2 文件上传安全
```csharp
// 1. 魔数校验（防止后缀伪装）
var fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();
var allowedTypes = new[] { "pdf", "txt", "docx" };
if (!allowedTypes.Contains(fileType))
    throw new BusinessException("不支持的文件类型");

// 2. 文件大小限制
if (fileSize > 100 * 1024 * 1024)  // 100MB
    throw new BusinessException("文件过大");

// 3. 文件名清洗（防止路径遍历）
fileName = Path.GetFileName(fileName);
fileName = Regex.Replace(fileName, @"[^\w\.-]", "_");

// 4. 病毒扫描（可选，使用ClamAV）
await _virusScanner.ScanAsync(fileStream);
```

#### 9.2.3 SQL注入防护
```csharp
// ❌ 不推荐：字符串拼接（SQL注入风险）
var sql = $"SELECT * FROM Documents WHERE Name = '{fileName}'";

// ✅ 推荐：参数化查询
var docs = await _db.Documents
    .Where(d => d.Name == fileName)
    .ToListAsync();
```

### 9.3 日志与监控

#### 9.3.1 结构化日志
```csharp
// 使用Serilog LogContext增强日志
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("TraceId", traceId))
{
    _logger.LogInformation("用户开始上传文档：{FileName}", fileName);
    
    try
    {
        await ProcessDocumentAsync(fileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "文档处理失败：{FileName}", fileName);
        throw;
    }
}
```

#### 9.3.2 指标采集
```csharp
// 使用Prometheus指标
public class MetricsService
{
    private static readonly Counter _documentUploads = Metrics
        .CreateCounter("rag_document_uploads_total", "文档上传总数");
    
    private static readonly Histogram _ragQueryDuration = Metrics
        .CreateHistogram("rag_query_duration_seconds", "RAG查询耗时");
    
    public void RecordDocumentUpload()
    {
        _documentUploads.Inc();
    }
    
    public IDisposable RecordQueryDuration()
    {
        return _ragQueryDuration.NewTimer();
    }
}
```

### 9.4 扩展性建议

#### 9.4.1 水平扩展
```yaml
# Docker Compose扩展副本
docker-compose up -d --scale enterpriserag-api=3

# Kubernetes HPA（自动扩缩容）
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: enterpriserag-api
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: enterpriserag-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

#### 9.4.2 Qdrant集群
```yaml
# docker-compose.yml
services:
  qdrant-node1:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    environment:
      - QDRANT__CLUSTER__ENABLED=true
      - QDRANT__CLUSTER__P2P__PORT=6335
  
  qdrant-node2:
    image: qdrant/qdrant:latest
    environment:
      - QDRANT__CLUSTER__ENABLED=true
      - QDRANT__CLUSTER__P2P__PORT=6335
      - QDRANT__CLUSTER__SEED_NODES=qdrant-node1:6335
```

---

## 10. 常见问题与排查

### Q1: 文档上传成功但问答无结果？
**排查步骤**：
1. 检查Qdrant是否正常初始化：`curl http://localhost:6333/collections/enterprise_rag_collection`
2. 检查向量是否插入成功：`curl http://localhost:6333/collections/enterprise_rag_collection/points/count`
3. 检查用户权限：查询`UserDocumentPermission`表
4. 检查日志：`docker logs enterpriserag-api | grep "RAG问答"`

### Q2: Python服务调用失败？
**排查步骤**：
1. 检查服务是否启动：`curl http://localhost:9000/health`
2. 检查网络连通性：`docker network inspect ai-enterpriserag_default`
3. 检查API Key配置：环境变量是否一致
4. 查看Python日志：`docker logs unstructured-parser`

### Q3: JWT认证失败？
**排查步骤**：
1. 检查Token是否过期：解码JWT查看`exp`字段
2. 检查SecretKey是否一致：`appsettings.json` vs `TokenService.cs`
3. 检查Claims格式：使用<https://jwt.io>解码Token
4. 检查权限Claim：`"perm": ["doc.read", "doc.upload"]`

### Q4: Agent推理陷入循环？
**排查步骤**：
1. 检查最大迭代次数：默认10轮
2. 检查LLM返回格式：是否包含`FinalAnswer`
3. 检查Prompt质量：是否明确告知结束条件
4. 降低temperature：从0.8降到0.5

---

## 11. 贡献指南

### 11.1 代码规范
- **C#**: 遵循Microsoft官方编码规范
- **Python**: 遵循PEP 8规范
- **Git提交**: 使用语义化提交消息（Conventional Commits）

### 11.2 提交流程
1. Fork仓库
2. 创建特性分支：`git checkout -b feature/your-feature`
3. 提交变更：`git commit -m "feat: 添加XXX功能"`
4. 推送分支：`git push origin feature/your-feature`
5. 创建Pull Request

### 11.3 测试要求
- 单元测试覆盖率 > 70%
- 集成测试覆盖核心流程
- 性能测试（RAG查询 < 5秒）

---

## 12. 许可证与联系方式

**许可证**: MIT License  
**GitHub**: <https://github.com/939481896/AI.EnterpriseRAG>  
**作者**: AI.EnterpriseRAG团队  
**邮箱**: support@enterpriserag.com  

---

**文档维护**：本文档持续更新，最新版本请查看GitHub仓库。
