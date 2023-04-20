using FluentValidation;

namespace j5.wtf.api.Validators;

public class IdValidator : AbstractValidator<string>
{
    public IdValidator()
    {
        RuleFor(id => id)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9]*$")
            .WithMessage("ID must contain only alphanumeric characters.");
    }
}