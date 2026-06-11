using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Resources;
using FluentValidation;

namespace AI.EnterpriseRAG.Application.Validators.User;

/// <summary>
/// 创建用户 DTO 验证器
/// </summary>
public class CreateUserDtoValidator : ValidatorBase<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("账号"))
            .Length(3, 50)
            .WithMessage(msg => $"账号长度必须在3到50个字符之间")
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage(MessageResources.Validation.AccountInvalid);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("密码"))
            .MinimumLength(8)
            .WithMessage(MessageResources.Validation.MinLength("密码", 8))
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage(MessageResources.Validation.PasswordWeak);

        RuleFor(x => x.RealName)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("姓名"))
            .MaximumLength(50)
            .WithMessage(MessageResources.Validation.MaxLength("姓名", 50));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage(MessageResources.Validation.EmailInvalid);

        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("手机号码格式不正确");
    }
}

/// <summary>
/// 更新用户 DTO 验证器
/// </summary>
public class UpdateUserDtoValidator : ValidatorBase<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.RealName)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("姓名"))
            .MaximumLength(50)
            .WithMessage(MessageResources.Validation.MaxLength("姓名", 50));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage(MessageResources.Validation.EmailInvalid);

        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("手机号码格式不正确");
    }
}

/// <summary>
/// 重置密码 DTO 验证器
/// </summary>
public class ResetPasswordDtoValidator : ValidatorBase<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("新密码"))
            .MinimumLength(8)
            .WithMessage(MessageResources.Validation.MinLength("新密码", 8))
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage(MessageResources.Validation.PasswordWeak);
    }
}

/// <summary>
/// 切换状态 DTO 验证器
/// </summary>
public class ToggleStatusDtoValidator : ValidatorBase<ToggleStatusDto>
{
    public ToggleStatusDtoValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull()
            .WithMessage(MessageResources.Validation.Required("状态"));
    }
}
