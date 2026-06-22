using FluentValidation;
using Rag.Contracts.Commands;

namespace Rag.Contracts.Validators;
public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.FileStream).NotNull().WithMessage("File is required");
        RuleFor(x => x.FileSize).GreaterThan(0).WithMessage("File cannot be empty");
        RuleFor(x => x.FileSize).LessThanOrEqualTo(50 * 1024 * 1024)
            .WithMessage("File size cannot exceed 50MB");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required");
    }
}

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message cannot be empty");
        RuleFor(x => x.Message).MaximumLength(10000).WithMessage("Message too long");
    }
}
