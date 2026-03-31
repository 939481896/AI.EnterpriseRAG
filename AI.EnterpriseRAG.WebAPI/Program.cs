using AI.EnterpriseRAG.Application.UseCases;
using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;
using AI.EnterpriseRAG.Infrastructure.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using AI.EnterpriseRAG.Infrastructure.Services.Llm;
using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using System;
using Xceed.Document.NET;
using AppEnterpriseAiContext = AI.EnterpriseRAG.Infrastructure.Persistence.AppEnterpriseAiContext;


Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ========== 1. 配置绑定 ==========
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("LlmOptions"));
builder.Services.Configure<VectorStoreOptions>(builder.Configuration.GetSection("VectorStoreOptions"));

// ========== 2. 数据库配置 ==========
builder.Services.AddDbContext<AI.EnterpriseRAG.Infrastructure.Persistence.AppEnterpriseAiContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30)),
        mysqloption => {
            mysqloption.EnableRetryOnFailure(); 
            mysqloption.MigrationsAssembly("AI.EnterpriseRAG.Infrastructure");
            mysqloption.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); // 禁用延迟加载

        });
    // 开发环境启用日志
    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// Add services to the container.
builder.Services.AddHttpClient<OllamaLlmService>();
builder.Services.AddHttpClient<TongyiLlmService>();

builder.Services.AddScoped<IDocumentRepository,DocumentRepository>();
builder.Services.AddScoped<IChatConversationRepository,ChatConversationRepository>();
//分块服务
builder.Services.AddScoped<DocumentChunkingService,DocumentChunkingService>();

//文档解析器
builder.Services.AddScoped<IDocumentParser,PdfDocumentParser>();
builder.Services.AddScoped<IDocumentParser,TxtDocumentParser>();



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


// 向量库服务
// ========== 向量库自动切换==========
var vectorStoreOptions = builder.Configuration.GetSection("VectorStoreOptions").Get<VectorStoreOptions>();
if (vectorStoreOptions == null)
    throw new Exception("VectorStoreOptions 配置不存在");

if (vectorStoreOptions.DefaultType.Equals("Chroma", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IVectorStore, ChromaVectorStore>();
    Console.WriteLine("已启用向量库：Chroma");
}
else if (vectorStoreOptions.DefaultType.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();
    Console.WriteLine("已启用向量库：Qdrant");
}
else
{
    throw new Exception("请配置 VectorStoreOptions:DefaultType = Chroma 或 Qdrant");
}

// 重排
builder.Services.AddHttpClient<IRerankService, BgeRerankService>();

// 权限 + 多租户
builder.Services.AddScoped<IPermissionService, PermissionRepository>();

// ========== 6. 用例注册 ==========
builder.Services.AddScoped<IDocumentUseCase, DocumentUseCase>();
builder.Services.AddScoped<IChatUseCase, ChatUseCase>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "企业级RAG智能问答API", Version = "v1" });
});

// ========== 8. 全局异常处理 ==========
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new BadRequestObjectResult(Result.Fail(string.Join("；", errors)));
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.

// ========== 中间件配置 ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "企业级RAG智能问答API v1"));
}

// 全局异常处理中间件
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BusinessException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = ex.Code;
        await context.Response.WriteAsJsonAsync(Result.Fail(ex.Message, ex.Code));
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(Result.Fail($"服务器内部错误：{ex.Message}", 500));
    }
});
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppEnterpriseAiContext>();
    await dbContext.Database.MigrateAsync(); // 自动应用迁移
}

app.Run();
