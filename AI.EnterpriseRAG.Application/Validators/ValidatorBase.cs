using FluentValidation;

namespace AI.EnterpriseRAG.Application.Validators;

/// <summary>
/// Validator 基类
/// 提供统一的验证配置和辅助方法
/// </summary>
public abstract class ValidatorBase<T> : AbstractValidator<T>
{
    protected ValidatorBase()
    {
        // 全局配置：继续执行所有验证规则（不在第一个错误时停止）
        ClassLevelCascadeMode = CascadeMode.Continue;
    }

    /// <summary>
    /// 使用资源文件的错误消息
    /// </summary>
    protected IRuleBuilderOptions<T, TProperty> WithLocalizedMessage<TProperty>(
        IRuleBuilderOptions<T, TProperty> rule, string messageKey)
    {
        return rule.WithMessage(Core.Resources.MessageResources.Get(messageKey));
    }

    /// <summary>
    /// 使用带参数的资源消息
    /// </summary>
    protected IRuleBuilderOptions<T, TProperty> WithLocalizedMessage<TProperty>(
        IRuleBuilderOptions<T, TProperty> rule, string messageKey, params object[] args)
    {
        return rule.WithMessage(Core.Resources.MessageResources.Get(messageKey, args));
    }
}
