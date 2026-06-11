using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Resources;
using FluentValidation;

namespace AI.EnterpriseRAG.Application.Validators.Chat;

/// <summary>
/// 聊天请求验证器
/// </summary>
public class ChatRequestValidator : ValidatorBase<ChatRequestDto>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(MessageResources.Validation.Required("用户ID"));

        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage(MessageResources.Chat.MessageEmpty)
            .MaximumLength(2000)
            .WithMessage(MessageResources.Validation.MaxLength("问题", 2000));
    }
}
