namespace AI.EnterpriseRAG.Core.Resources;

/// <summary>
/// 统一消息资源管理（支持多语言国际化）
/// 企业级消息管理中心 - 所有用户可见的消息都应该从这里获取
/// </summary>
public static class MessageResources
{
    private static readonly Dictionary<string, Dictionary<string, string>> Messages = new()
    {
        ["zh-CN"] = new()
        {
            // ==================== Authentication & Authorization ====================
            ["auth.login.success"] = "登录成功",
            ["auth.login.invalid_credentials"] = "账号或密码错误",
            ["auth.login.account_disabled"] = "账号已被禁用，请联系管理员",
            ["auth.login.failed"] = "登录失败，请稍后重试",
            ["auth.logout.success"] = "已退出登录",
            ["auth.token.expired"] = "令牌已过期，请重新登录",
            ["auth.token.invalid"] = "令牌无效",
            ["auth.unauthorized"] = "未授权或令牌过期",
            ["auth.permission_denied"] = "权限不足",
            ["auth.refresh_token.invalid"] = "刷新令牌无效或已过期",
            
            // ==================== User Management ====================
            ["user.create.success"] = "用户创建成功",
            ["user.create.account_exists"] = "账号已存在",
            ["user.update.success"] = "用户信息已更新",
            ["user.delete.success"] = "用户已删除",
            ["user.delete.cannot_delete_admin"] = "不能删除管理员账号",
            ["user.status.enabled"] = "用户已启用",
            ["user.status.disabled"] = "用户已禁用",
            ["user.status.cannot_disable_admin"] = "不能禁用管理员账号",
            ["user.password.reset_success"] = "密码已重置",
            ["user.notfound"] = "用户不存在",
            
            // ==================== Document Management ====================
            ["document.upload.success"] = "文档上传成功",
            ["document.upload.failed"] = "文档上传失败",
            ["document.upload.processing"] = "文档处理中",
            ["document.upload.duplicate"] = "文档已存在",
            ["document.delete.success"] = "文档已删除",
            ["document.notfound"] = "文档不存在",
            ["document.parse.failed"] = "文档解析失败",
            ["document.permission.denied"] = "无权访问此文档",
            
            // ==================== Chat & Conversation ====================
            ["chat.send.failed"] = "发送失败，请重试",
            ["chat.session.created"] = "会话创建成功",
            ["chat.session.deleted"] = "会话已删除",
            ["chat.session.notfound"] = "会话不存在",
            ["chat.session.title_updated"] = "会话标题已更新",
            ["chat.message.empty"] = "消息内容不能为空",
            
            // ==================== Agent ====================
            ["agent.execution.started"] = "Agent 执行已启动",
            ["agent.execution.completed"] = "Agent 执行完成",
            ["agent.execution.failed"] = "Agent 执行失败",
            ["agent.tool.not_found"] = "工具不存在",
            ["agent.session.notfound"] = "Agent 会话不存在",
            
            // ==================== Validation Messages ====================
            ["validation.required"] = "{0}不能为空",
            ["validation.min_length"] = "{0}最少{1}个字符",
            ["validation.max_length"] = "{0}最多{1}个字符",
            ["validation.range"] = "{0}必须在{1}到{2}之间",
            ["validation.email.invalid"] = "邮箱格式不正确",
            ["validation.password.weak"] = "密码强度不足，需包含大小写字母、数字，且长度至少8位",
            ["validation.account.invalid"] = "账号只能包含字母、数字和下划线",
            ["validation.invalid_format"] = "{0}格式不正确",
            ["validation.file.too_large"] = "文件大小不能超过{0}MB",
            ["validation.file.invalid_type"] = "不支持的文件类型",
            
            // ==================== Common Messages ====================
            ["common.operation_success"] = "操作成功",
            ["common.operation_failed"] = "操作失败",
            ["common.save_success"] = "保存成功",
            ["common.delete_success"] = "删除成功",
            ["common.update_success"] = "更新成功",
            ["common.parameter_error"] = "参数验证失败",
            ["common.system_error"] = "系统错误，请稍后重试",
            ["common.network_error"] = "网络错误，请检查连接",
            ["common.timeout"] = "请求超时",
            ["common.notfound"] = "资源不存在",
        },
        
        ["en-US"] = new()
        {
            // ==================== Authentication & Authorization ====================
            ["auth.login.success"] = "Login successful",
            ["auth.login.invalid_credentials"] = "Invalid account or password",
            ["auth.login.account_disabled"] = "Account has been disabled, please contact administrator",
            ["auth.login.failed"] = "Login failed, please try again later",
            ["auth.logout.success"] = "Logged out successfully",
            ["auth.token.expired"] = "Token expired, please login again",
            ["auth.token.invalid"] = "Invalid token",
            ["auth.unauthorized"] = "Unauthorized or token expired",
            ["auth.permission_denied"] = "Permission denied",
            ["auth.refresh_token.invalid"] = "Invalid or expired refresh token",
            
            // ==================== User Management ====================
            ["user.create.success"] = "User created successfully",
            ["user.create.account_exists"] = "Account already exists",
            ["user.update.success"] = "User updated successfully",
            ["user.delete.success"] = "User deleted successfully",
            ["user.delete.cannot_delete_admin"] = "Cannot delete admin account",
            ["user.status.enabled"] = "User enabled",
            ["user.status.disabled"] = "User disabled",
            ["user.status.cannot_disable_admin"] = "Cannot disable admin account",
            ["user.password.reset_success"] = "Password reset successfully",
            ["user.notfound"] = "User not found",
            
            // ==================== Document Management ====================
            ["document.upload.success"] = "Document uploaded successfully",
            ["document.upload.failed"] = "Document upload failed",
            ["document.upload.processing"] = "Document processing",
            ["document.upload.duplicate"] = "Document already exists",
            ["document.delete.success"] = "Document deleted successfully",
            ["document.notfound"] = "Document not found",
            ["document.parse.failed"] = "Document parsing failed",
            ["document.permission.denied"] = "No permission to access this document",
            
            // ==================== Chat & Conversation ====================
            ["chat.send.failed"] = "Send failed, please try again",
            ["chat.session.created"] = "Session created successfully",
            ["chat.session.deleted"] = "Session deleted successfully",
            ["chat.session.notfound"] = "Session not found",
            ["chat.session.title_updated"] = "Session title updated",
            ["chat.message.empty"] = "Message content cannot be empty",
            
            // ==================== Agent ====================
            ["agent.execution.started"] = "Agent execution started",
            ["agent.execution.completed"] = "Agent execution completed",
            ["agent.execution.failed"] = "Agent execution failed",
            ["agent.tool.not_found"] = "Tool not found",
            ["agent.session.notfound"] = "Agent session not found",
            
            // ==================== Validation Messages ====================
            ["validation.required"] = "{0} is required",
            ["validation.min_length"] = "{0} must be at least {1} characters",
            ["validation.max_length"] = "{0} must not exceed {1} characters",
            ["validation.range"] = "{0} must be between {1} and {2}",
            ["validation.email.invalid"] = "Invalid email format",
            ["validation.password.weak"] = "Password must contain uppercase, lowercase, digits and be at least 8 characters",
            ["validation.account.invalid"] = "Account can only contain letters, numbers and underscores",
            ["validation.invalid_format"] = "Invalid {0} format",
            ["validation.file.too_large"] = "File size cannot exceed {0}MB",
            ["validation.file.invalid_type"] = "Unsupported file type",
            
            // ==================== Common Messages ====================
            ["common.operation_success"] = "Operation successful",
            ["common.operation_failed"] = "Operation failed",
            ["common.save_success"] = "Saved successfully",
            ["common.delete_success"] = "Deleted successfully",
            ["common.update_success"] = "Updated successfully",
            ["common.parameter_error"] = "Parameter validation failed",
            ["common.system_error"] = "System error, please try again later",
            ["common.network_error"] = "Network error, please check connection",
            ["common.timeout"] = "Request timeout",
            ["common.notfound"] = "Resource not found",
        }
    };

    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 设置当前语言
    /// </summary>
    public static void SetLanguage(string language)
    {
        if (Messages.ContainsKey(language))
        {
            _currentLanguage = language;
        }
    }

    /// <summary>
    /// 获取当前语言
    /// </summary>
    public static string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// 获取消息（支持格式化参数）
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        if (Messages.TryGetValue(_currentLanguage, out var lang) && 
            lang.TryGetValue(key, out var message))
        {
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        
        // Fallback: 如果消息不存在，返回 key 并记录警告
        Console.WriteLine($"[MessageResources] Missing message key: {key} for language: {_currentLanguage}");
        return key;
    }

    /// <summary>
    /// 检查消息键是否存在
    /// </summary>
    public static bool Exists(string key)
    {
        return Messages.TryGetValue(_currentLanguage, out var lang) && lang.ContainsKey(key);
    }

    // ==================== 便捷访问类（强类型，IDE 智能提示） ====================
    
    /// <summary>
    /// 认证相关消息
    /// </summary>
    public static class Auth
    {
        public static string LoginSuccess => Get("auth.login.success");
        public static string InvalidCredentials => Get("auth.login.invalid_credentials");
        public static string AccountDisabled => Get("auth.login.account_disabled");
        public static string LoginFailed => Get("auth.login.failed");
        public static string LogoutSuccess => Get("auth.logout.success");
        public static string TokenExpired => Get("auth.token.expired");
        public static string TokenInvalid => Get("auth.token.invalid");
        public static string Unauthorized => Get("auth.unauthorized");
        public static string PermissionDenied => Get("auth.permission_denied");
        public static string RefreshTokenInvalid => Get("auth.refresh_token.invalid");
    }

    /// <summary>
    /// 用户管理消息
    /// </summary>
    public static class User
    {
        public static string CreateSuccess => Get("user.create.success");
        public static string AccountExists => Get("user.create.account_exists");
        public static string UpdateSuccess => Get("user.update.success");
        public static string DeleteSuccess => Get("user.delete.success");
        public static string CannotDeleteAdmin => Get("user.delete.cannot_delete_admin");
        public static string StatusEnabled => Get("user.status.enabled");
        public static string StatusDisabled => Get("user.status.disabled");
        public static string CannotDisableAdmin => Get("user.status.cannot_disable_admin");
        public static string PasswordResetSuccess => Get("user.password.reset_success");
        public static string NotFound => Get("user.notfound");
    }

    /// <summary>
    /// 文档管理消息
    /// </summary>
    public static class Document
    {
        public static string UploadSuccess => Get("document.upload.success");
        public static string UploadFailed => Get("document.upload.failed");
        public static string Processing => Get("document.upload.processing");
        public static string Duplicate => Get("document.upload.duplicate");
        public static string DeleteSuccess => Get("document.delete.success");
        public static string NotFound => Get("document.notfound");
        public static string ParseFailed => Get("document.parse.failed");
        public static string PermissionDenied => Get("document.permission.denied");
    }

    /// <summary>
    /// 聊天会话消息
    /// </summary>
    public static class Chat
    {
        public static string SendFailed => Get("chat.send.failed");
        public static string SessionCreated => Get("chat.session.created");
        public static string SessionDeleted => Get("chat.session.deleted");
        public static string SessionNotFound => Get("chat.session.notfound");
        public static string TitleUpdated => Get("chat.session.title_updated");
        public static string MessageEmpty => Get("chat.message.empty");
    }

    /// <summary>
    /// Agent 消息
    /// </summary>
    public static class Agent
    {
        public static string ExecutionStarted => Get("agent.execution.started");
        public static string ExecutionCompleted => Get("agent.execution.completed");
        public static string ExecutionFailed => Get("agent.execution.failed");
        public static string ToolNotFound => Get("agent.tool.not_found");
        public static string SessionNotFound => Get("agent.session.notfound");
    }

    /// <summary>
    /// 验证消息（支持格式化）
    /// </summary>
    public static class Validation
    {
        public static string Required(string fieldName) => Get("validation.required", fieldName);
        public static string MinLength(string fieldName, int length) => Get("validation.min_length", fieldName, length);
        public static string MaxLength(string fieldName, int length) => Get("validation.max_length", fieldName, length);
        public static string Range(string fieldName, int min, int max) => Get("validation.range", fieldName, min, max);
        public static string EmailInvalid => Get("validation.email.invalid");
        public static string PasswordWeak => Get("validation.password.weak");
        public static string AccountInvalid => Get("validation.account.invalid");
        public static string InvalidFormat(string fieldName) => Get("validation.invalid_format", fieldName);
        public static string FileTooLarge(int maxSizeMB) => Get("validation.file.too_large", maxSizeMB);
        public static string FileInvalidType => Get("validation.file.invalid_type");
    }

    /// <summary>
    /// 通用消息
    /// </summary>
    public static class Common
    {
        public static string OperationSuccess => Get("common.operation_success");
        public static string OperationFailed => Get("common.operation_failed");
        public static string SaveSuccess => Get("common.save_success");
        public static string DeleteSuccess => Get("common.delete_success");
        public static string UpdateSuccess => Get("common.update_success");
        public static string ParameterError => Get("common.parameter_error");
        public static string SystemError => Get("common.system_error");
        public static string NetworkError => Get("common.network_error");
        public static string Timeout => Get("common.timeout");
        public static string NotFound => Get("common.notfound");
    }
}
