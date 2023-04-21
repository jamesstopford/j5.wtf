using FluentValidation;
using j5.wtf.api.Models;

namespace j5.wtf.api.Validators;

public class DestinationUrlValidator : AbstractValidator<UrlInput>
{
    public DestinationUrlValidator()
    {
        RuleFor(x => x.DestinationUrl)
            .NotEmpty().WithMessage("URL is required.")
            .Must(BeAValidUrl).WithMessage("URL must be valid.");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}