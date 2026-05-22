using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// 邮件分类和工单处理工具（智能客服核心）
/// </summary>
public class EmailTicketTool : ITool
{
    private readonly ILogger<EmailTicketTool> _logger;
    private readonly HttpClient _httpClient;

    public string Name => "email_ticket_handler";
    
    public string Description => @"智能处理邮件和工单，包括：
1. 邮件分类（紧急/重要/普通）
2. 提取关键信息（订单号、联系方式、问题描述）
3. 自动创建工单并分配
4. 生成回复草稿
适用场景：客服邮件处理、售后工单管理";

    public string Category => "customer_service";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""action"": {
      ""type"": ""string"",
      ""enum"": [""classify"", ""extract_info"", ""create_ticket"", ""generate_reply""],
      ""description"": ""操作类型：classify(分类)、extract_info(提取信息)、create_ticket(创建工单)、generate_reply(生成回复)""
    },
    ""email_content"": {
      ""type"": ""string"",
      ""description"": ""邮件正文内容""
    },
    ""email_subject"": {
      ""type"": ""string"",
      ""description"": ""邮件主题（可选）""
    },
    ""sender_email"": {
      ""type"": ""string"",
      ""description"": ""发件人邮箱（可选）""
    }
  },
  ""required"": [""action"", ""email_content""]
}";

    public EmailTicketTool(ILogger<EmailTicketTool> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. 参数验证
            if (!arguments.TryGetValue("action", out var actionObj) ||
                !arguments.TryGetValue("email_content", out var contentObj))
            {
                return ToolResult.Failure("缺少必需参数: action, email_content");
            }

            var action = actionObj.ToString()!;
            var emailContent = contentObj.ToString()!;
            var emailSubject = arguments.TryGetValue("email_subject", out var subjectObj)
                ? subjectObj.ToString()
                : "";
            var senderEmail = arguments.TryGetValue("sender_email", out var senderObj)
                ? senderObj.ToString()
                : "";

            _logger.LogInformation(
                "用户 {UserId} 执行邮件处理: Action={Action}",
                context.UserId, action);

            // 2. 根据操作类型执行
            object result = action switch
            {
                "classify" => ClassifyEmail(emailContent, emailSubject),
                "extract_info" => ExtractKeyInfo(emailContent, emailSubject),
                "create_ticket" => await CreateTicketAsync(emailContent, emailSubject, senderEmail, context, cancellationToken),
                "generate_reply" => GenerateReplyDraft(emailContent, emailSubject),
                _ => throw new ArgumentException($"不支持的操作类型: {action}")
            };

            sw.Stop();

            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    action,
                    result,
                    processed_at = DateTime.UtcNow
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件处理失败");
            return ToolResult.Failure($"邮件处理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 邮件分类（基于规则，生产环境建议用LLM）
    /// </summary>
    private object ClassifyEmail(string content, string? subject)
    {
        var text = (subject + " " + content).ToLower();

        // 紧急关键词
        var urgentKeywords = new[] { "紧急", "urgent", "asap", "立即", "马上", "故障", "宕机", "无法访问" };
        var isUrgent = urgentKeywords.Any(k => text.Contains(k));

        // 重要关键词
        var importantKeywords = new[] { "投诉", "退款", "合同", "法律", "损失", "严重" };
        var isImportant = importantKeywords.Any(k => text.Contains(k));

        // 问题类型识别
        var category = "general";
        if (text.Contains("订单") || text.Contains("发货") || text.Contains("物流"))
            category = "order";
        else if (text.Contains("退款") || text.Contains("退货") || text.Contains("换货"))
            category = "refund";
        else if (text.Contains("产品") || text.Contains("质量") || text.Contains("故障"))
            category = "product_issue";
        else if (text.Contains("账号") || text.Contains("登录") || text.Contains("密码"))
            category = "account";
        else if (text.Contains("咨询") || text.Contains("了解") || text.Contains("请问"))
            category = "inquiry";

        var priority = isUrgent ? "urgent" : (isImportant ? "important" : "normal");

        return new
        {
            priority,
            category,
            is_urgent = isUrgent,
            is_important = isImportant,
            suggested_department = GetDepartmentByCategory(category),
            confidence = 0.85f
        };
    }

    /// <summary>
    /// 提取关键信息
    /// </summary>
    private object ExtractKeyInfo(string content, string? subject)
    {
        var info = new Dictionary<string, object>();

        // 提取订单号（格式：ORD123456、#123456、订单号：123456）
        var orderPattern = @"(?:订单号[:：]?\s*|ORD|#)(\w{6,20})";
        var orderMatch = Regex.Match(content, orderPattern, RegexOptions.IgnoreCase);
        if (orderMatch.Success)
        {
            info["order_number"] = orderMatch.Groups[1].Value;
        }

        // 提取联系电话
        var phonePattern = @"1[3-9]\d{9}";
        var phoneMatch = Regex.Match(content, phonePattern);
        if (phoneMatch.Success)
        {
            info["phone"] = phoneMatch.Value;
        }

        // 提取邮箱
        var emailPattern = @"[\w\.-]+@[\w\.-]+\.\w+";
        var emailMatch = Regex.Match(content, emailPattern);
        if (emailMatch.Success)
        {
            info["email"] = emailMatch.Value;
        }

        // 提取金额
        var amountPattern = @"(?:¥|￥|\$|RMB)\s*(\d+(?:\.\d{2})?)";
        var amountMatch = Regex.Match(content, amountPattern);
        if (amountMatch.Success)
        {
            info["amount"] = amountMatch.Groups[1].Value;
        }

        // 问题描述（取第一段非空文本）
        var lines = content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 10)
            .Take(3)
            .ToList();
        if (lines.Any())
        {
            info["problem_description"] = string.Join(" ", lines);
        }

        return new
        {
            extracted_fields = info,
            field_count = info.Count
        };
    }

    /// <summary>
    /// 创建工单（示例实现）
    /// </summary>
    private async Task<object> CreateTicketAsync(
        string content,
        string? subject,
        string? senderEmail,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        // 1. 先分类
        var classification = ClassifyEmail(content, subject);
        var classData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(classification));

        // 2. 提取信息
        var extractedInfo = ExtractKeyInfo(content, subject);

        // 3. 生成工单ID
        var ticketId = $"TKT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // 4. 创建工单（实际应写入数据库）
        var ticket = new
        {
            ticket_id = ticketId,
            subject = subject ?? "无主题",
            content,
            sender_email = senderEmail,
            priority = classData?["priority"].GetString(),
            category = classData?["category"].GetString(),
            assigned_department = classData?["suggested_department"].GetString(),
            status = "open",
            created_by = context.UserId,
            created_at = DateTime.UtcNow,
            extracted_info = extractedInfo
        };

        _logger.LogInformation(
            "创建工单: {TicketId}, Priority={Priority}",
            ticketId, ticket.priority);

        // TODO: 实际保存到数据库
        // await _ticketRepository.CreateAsync(ticket);

        // TODO: 发送通知给对应部门
        // await _notificationService.NotifyDepartmentAsync(ticket.assigned_department, ticket);

        return new
        {
            success = true,
            ticket,
            message = $"工单 {ticketId} 已创建并分配给 {ticket.assigned_department}"
        };
    }

    /// <summary>
    /// 生成回复草稿
    /// </summary>
    private object GenerateReplyDraft(string content, string? subject)
    {
        // 简化实现（生产环境应使用LLM生成个性化回复）
        var classification = ClassifyEmail(content, subject);
        var classData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(classification));
        var category = classData?["category"].GetString();

        var replyTemplate = category switch
        {
            "order" => @"尊敬的客户，您好！

感谢您的来信。关于您提到的订单问题，我们已经收到并正在处理中。

我们会在24小时内核实订单状态，并及时与您联系。如有紧急情况，请拨打客服热线：400-XXX-XXXX。

再次感谢您的理解与支持！

此致
客服团队",

            "refund" => @"尊敬的客户，您好！

我们已收到您的退款申请。

根据您的情况，我们会在3-5个工作日内处理完毕，退款将原路返回您的支付账户。

如有疑问，请随时联系我们。

此致
客服团队",

            "product_issue" => @"尊敬的客户，您好！

非常抱歉给您带来不便。

关于您反馈的产品问题，我们已转交技术部门优先处理。工程师会在24小时内联系您，协助解决问题。

感谢您的耐心等待！

此致
客服团队",

            _ => @"尊敬的客户，您好！

感谢您的来信，我们已收到您的反馈。

我们的客服团队会尽快处理您的问题，并在24小时内与您联系。

如有紧急情况，请拨打客服热线：400-XXX-XXXX。

此致
客服团队"
        };

        return new
        {
            reply_draft = replyTemplate,
            suggested_subject = $"Re: {subject}",
            tone = "polite",
            requires_review = classData?["priority"].GetString() == "urgent"
        };
    }

    private string GetDepartmentByCategory(string category)
    {
        return category switch
        {
            "order" => "订单部",
            "refund" => "财务部",
            "product_issue" => "技术支持部",
            "account" => "IT部",
            "inquiry" => "客服部",
            _ => "客服部"
        };
    }
}
