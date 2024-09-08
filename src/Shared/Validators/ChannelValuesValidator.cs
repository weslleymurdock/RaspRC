using FluentValidation;
using Shared.Models;

namespace Shared.Validators;

public class ChannelValuesValidator : AbstractValidator<Channel>
{
    public ChannelValuesValidator()
    {
        RuleFor(c => c.Value)
            .NotEmpty()
            .WithMessage("Channels require 8 values");

        RuleFor(c => c.Value)
            .Must(x => x.Length == 8)
            .WithMessage("Channels require 8 inputs");
        
        RuleForEach(c => c.Value)
            .InclusiveBetween(1000,2000)
            .WithMessage("All Channels must be at least 1000 and max 2000");

        RuleFor(c => c.HexValue)
            .NotNull()
            .WithMessage("The Channels string must have values");

        RuleFor(c => c.HexValue)
            .Must(x => x.Length == 24)
            .WithMessage("The Channels string must have 24 hex digits");
        
        RuleFor(c => c.HexValue)
            .Must(x => int.TryParse(x, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int i))
            .WithMessage("The Channels string has to be an hex string");
    }
}
