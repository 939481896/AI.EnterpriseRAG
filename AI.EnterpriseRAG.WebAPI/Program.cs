using AI.EnterpriseRAG.Application.Authorization;
using AI.EnterpriseRAG.Application.UseCases;
using AI.EnterpriseRAG.Application.Services; // 新增：并发控制
using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using AI.EnterpriseRAG.Infrastructure.Authorization;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using AI.EnterpriseRAG.Infrastructure.Middleware;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;
using AI.EnterpriseRAG.Infrastructure.Security;
using AI.EnterpriseRAG.Infrastructure.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using AI.EnterpriseRAG.Infrastructure.Services.Llm;
using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using AI.EnterpriseRAG.Infrastructure.Services.Agent;
using AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;
using AI.EnterpriseRAG.WebAPI;
using AI.EnterpriseRAG.WebAPI.Middleware; // 🆕 开发环境中间件
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens; // 🆕 添加JWT扩展
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting; // 🆕 Serilog.AspNetCore 扩展
using System.Text;
using Serilog.Sinks.File;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine($"当前工作目录: {Directory.GetCurrentDirectory()}");

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 📝 Serilog配置（完整版，集成 ASP.NET Core 日志）
// ============================================
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()  // 全局Debug级别
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // 🆕 过滤ASP.NET Core日志

    // 控制台输出（带详细信息）
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

    // 完整日志文件
    .WriteTo.File(
        path: "Logs/app-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Debug,
        fileSizeLimitBytes: 1024 * 1024 * 100,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 30,
        encoding: System.Text.Encoding.UTF8,
        shared: true, // 🆕 允许多进程写入
        flushToDiskInterval: TimeSpan.FromSeconds(1), // 🆕 每秒刷新到磁盘
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
    )

    // 错误日志单独文件
    .WriteTo.File(
        path: "Logs/errors-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Error,
        retainedFileCountLimit: 90,
        encoding: System.Text.Encoding.UTF8,
        shared: true, // 🆕 允许多进程写入
        flushToDiskInterval: TimeSpan.FromSeconds(1), // 🆕 每秒刷新到磁盘
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}]{NewLine}{SourceContext}: {Message:lj}{NewLine}{Exception}{NewLine}────────────────────────────────────{NewLine}"
    )

    .CreateLogger();

Log.Information("🚀 应用程序启动中...");

// 将 Serilog 集成到 ASP.NET Core 日志系统
builder.Host.UseSerilog();

// 1. JWT 鉴权（统一配置 + 正确解析 Claim）
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,

            // 【必须和 TokenService 里完全一致】
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "rag.auth",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "rag.api",

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ??
                throw new InvalidOperationException("JWT 密钥未配置"))),

            // 🆕 让系统正确识别 Claim（与 TokenService 保持一致）
            NameClaimType = JwtRegisteredClaimNames.UniqueName, // 🆕 使用 UniqueName（Account）
            RoleClaimType = "perm", // 权限用 perm 作为 Claim
        };

        // 令牌过期返回详细信息
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            }
        };
    });

// 2.授权策略（权限生效核心）
//静态权限校验
/*
builder.Services.AddAuthorization(options =>
{

    /*options.AddPolicy("chat.ask", policy => policy.RequireClaim("perm", "chat.ask"));
    options.AddPolicy("doc.read", policy => policy.RequireClaim("perm", "doc.read"));
    options.AddPolicy("doc.upload", policy => policy.RequireClaim("perm", "doc.upload"));
    // 全局动态策略提供器

});*/

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();


//注册哈希加密解密器
builder.Services.AddScoped<IPasswordHasher<SysUser>, PasswordHasher<SysUser>>();

// ========== 1. 配置绑定 ==========
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("LlmOptions"));
builder.Services.Configure<VectorStoreOptions>(builder.Configuration.GetSection("VectorStoreOptions"));

// ========== 2. 数据库配置 ==========
builder.Services.AddDbContext<AppEnterpriseAiContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30)),
        mysql =>
        {
            mysql.EnableRetryOnFailure();
            mysql.MigrationsAssembly("AI.EnterpriseRAG.Infrastructure");
            mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// ========== 3. 基础服务 ==========
builder.Services.AddHttpClient<OllamaLlmService>();
builder.Services.AddHttpClient<TongyiLlmService>();
builder.Services.AddHttpClient<IRerankService, BgeRerankService>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
builder.Services.AddScoped<DocumentChunkingService>();

// 文档解析器
builder.Services.AddScoped<IDocumentParser, PdfDocumentParser>();
builder.Services.AddScoped<IDocumentParser, TxtDocumentParser>();

// 大模型自动切换
// 大模型服务（企业级多模型切换）
var llmOptions = builder.Configuration.GetSection("LlmOptions").Get<LlmOptions>();
if (llmOptions?.DefaultModel == "ollama")
{
    builder.Services.AddScoped<ILlmService, OllamaLlmService>();
}
else if (llmOptions?.DefaultModel == "tongyi")
{
    builder.Services.AddScoped<ILlmService, TongyiLlmService>();
}
else
{
    builder.Services.AddScoped<ILlmService, OllamaLlmService>();
}
builder.Services.AddScoped<UnstructuredClient>();

// 向量库自动切换
var vectorStoreOptions = builder.Configuration.GetSection("VectorStoreOptions").Get<VectorStoreOptions>();
if (vectorStoreOptions == null) throw new Exception("VectorStoreOptions 未配置");

builder.Services.AddScoped<IVectorStore>(sp =>
    vectorStoreOptions.DefaultType.Equals("Qdrant", StringComparison.OrdinalIgnoreCase)
        ? new QdrantVectorStore(
            sp.GetRequiredService<IOptions<VectorStoreOptions>>(),
            sp.GetRequiredService<ILogger<QdrantVectorStore>>())
        : new ChromaVectorStore(sp.GetRequiredService<IOptions<VectorStoreOptions>>()));

// 注册认证 & 令牌服务
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

// 权限仓储
builder.Services.AddScoped<IPermissionService, PermissionRepository>();

// 🆕 细粒度权限服务
builder.Services.AddScoped<IFineGrainedPermissionService, FineGrainedPermissionService>();

// ========== 5. Agent智能体服务注册 ==========
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddScoped<IIntentRecognitionService, IntentRecognitionService>();
builder.Services.AddScoped<IAgentOrchestrator, ReactAgentOrchestrator>();

// 注册Agent工具
builder.Services.AddScoped<RagSearchTool>();
builder.Services.AddScoped<DataCollectionTool>();
builder.Services.AddScoped<LogAnalysisTool>();

// 新增3个企业级工具
builder.Services.AddScoped<SqlQueryTool>();
builder.Services.AddScoped<EmailTicketTool>();
builder.Services.AddScoped<ServerMonitorTool>();

// 工具注册初始化（启动时自动注册）
builder.Services.AddHostedService<ToolRegistrationService>();

// 🆕 文档恢复服务（启动时自动恢复未完成的文档）
builder.Services.AddHostedService<DocumentRecoveryService>();

// ========== 6. 用例注册 ==========
builder.Services.AddScoped<IDocumentUseCase, DocumentUseCase>();
builder.Services.AddScoped<IChatUseCase, ChatUseCase>();

// 文档处理并发控制（单例，全局限流）
builder.Services.AddSingleton<DocumentProcessingThrottler>(sp =>
    new DocumentProcessingThrottler(
        logger: sp.GetRequiredService<ILogger<DocumentProcessingThrottler>>(),
        maxConcurrency: 3)); // 最多同时处理3个文档

// ========== API & Swagger ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "企业级RAG智能问答API", Version = "v1" });

    // Swagger 支持 JWT 授权
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "bearer",
        Description = "输入 Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Id = "Bearer", Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme }
            },
            Array.Empty<string>()
        }
    });
});

// 全局模型验证
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = string.Join("；", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        return new BadRequestObjectResult(Result.Fail(errors));
    };
});

var app = builder.Build();




// ========== 中间件顺序（固定标准） ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // 🆕 开发环境自动认证（调试时自动登录为admin）
    // 启用后，所有请求都会自动注入admin用户身份，无需token
    // 如需测试真实JWT验证，注释此行
    app.UseDevAutoAuth();
    Log.Information("✅ 开发环境自动认证已启用（自动以admin身份登录）");
}
app.UseMiddleware<GlobalLogMiddleware>();
app.UseMiddleware<PermissionAuditMiddleware>();
// 全局异常处理
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BusinessException ex)
    {
        context.Response.StatusCode = ex.Code;
        Log.Error(ex, "业务异常：{Msg}", ex.Message); // 改为 Serilog

        await context.Response.WriteAsJsonAsync(Result.Fail(ex.Message, ex.Code));
    }
    catch (UnauthorizedAccessException Uae)
    {
        context.Response.StatusCode = 401;
        Log.Warning(Uae, "未授权访问");

        await context.Response.WriteAsJsonAsync(Result.Fail("未授权或令牌过期", 401));
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        Log.Error(ex, "全局异常：{Msg}", ex.Message);
        await context.Response.WriteAsJsonAsync(Result.Fail($"服务器错误：{ex.Message}", 500));
    }
});

app.UseHttpsRedirection();

// 顺序必须正确：先认证 → 后授权
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 自动迁移数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppEnterpriseAiContext>();
    await db.Database.MigrateAsync();

    // 🆕 初始化Qdrant Collection（启动时确保Collection存在）
    try 
    {
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        await vectorStore.InitAsync();
        Log.Information("✅ Qdrant Collection 初始化成功");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ Qdrant Collection 初始化失败（Qdrant可能未启动）");
        // 不阻止应用启动，允许在没有Qdrant的情况下运行（仅影响向量功能）
    }
}

try
{
    Log.Information("✅ 应用程序启动成功");
    Log.Information("📋 当前日志配置：");
    Log.Information("  - 控制台日志：启用");
    Log.Information("  - 文件日志：Logs/app-yyyyMMdd.log");
    Log.Information("  - 错误日志：Logs/errors-yyyyMMdd.log");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ 应用程序启动失败");
    throw;
}
finally
{
    Log.Information("🛑 应用程序正在关闭...");
    await Log.CloseAndFlushAsync(); // 异步刷新
}
