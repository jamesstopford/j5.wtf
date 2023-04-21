using FluentValidation;
using FluentValidation.Validators;

namespace j5.wtf.api.Validators;

public class IdValidator : AbstractValidator<string>
{
    public IdValidator()
    {
        RuleFor(id => id)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9]*$")
            .WithMessage("ID must contain only alphanumeric characters.");

        RuleFor(id => id)
            .NotEmpty()
            .Must(x => x.Length <= 10)
            .WithMessage("ID must be 10 characters or less.");
    }
}