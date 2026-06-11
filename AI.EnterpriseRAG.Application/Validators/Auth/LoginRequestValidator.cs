using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Resources;
using FluentValidation;

namespace AI.EnterpriseRAG.Application.Validators.Auth;

/// <summary>
/// 登录请求验证器
/// </summary>
public class LoginRequestValidator : ValidatorBase<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("账号"))
            .Length(3, 50)
            .WithMessage(MessageResources.Validation.MinLength("账号", 3));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("密码"))
            .MinimumLength(6)
            .WithMessage(MessageResources.Validation.MinLength("密码", 6));

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("租户ID"));
    }
}
