using AI.EnterpriseRAG.Application.Authorization;
using AI.EnterpriseRAG.Application.UseCases;
using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Authorization;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;
using AI.EnterpriseRAG.Infrastructure.Security;
using AI.EnterpriseRAG.Infrastructure.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using AI.EnterpriseRAG.Infrastructure.Services.Llm;
using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

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

            // 【关键修复】让系统正确识别 Claim
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
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
        ? new QdrantVectorStore(sp.GetRequiredService<IOptions<VectorStoreOptions>>())
        : new ChromaVectorStore(sp.GetRequiredService<IOptions<VectorStoreOptions>>()));

// 注册认证 & 令牌服务
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

// 权限仓储
builder.Services.AddScoped<IPermissionService, PermissionRepository>();
builder.Services.AddScoped<IRerankService,BgeRerankService>();

// ========== 6. 用例注册 ==========
builder.Services.AddScoped<IDocumentUseCase, DocumentUseCase>();
builder.Services.AddScoped<IChatUseCase, ChatUseCase>();

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
}

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
        await context.Response.WriteAsJsonAsync(Result.Fail(ex.Message, ex.Code));
    }
    catch (UnauthorizedAccessException)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(Result.Fail("未授权或令牌过期", 401));
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
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
}

app.Run();
