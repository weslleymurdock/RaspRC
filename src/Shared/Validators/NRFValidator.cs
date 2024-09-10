using FluentValidation;
using Shared.Models;

namespace Shared.Validators;

public class NRFValidator : AbstractValidator<NRF24>
{
    public NRFValidator()
    {
        RuleFor(nrf => nrf.Channel)
            .NotNull()
            .NotEmpty()
            .InclusiveBetween(0, 125)
            .WithMessage("The Channel must be not null, not empty, and must be in range of 0 to 125");

        RuleFor(nrf => nrf.RXAddress)
            .NotNull()
            .NotEmpty()
            .Length(24)
            .Matches(@"^([0][x][0-9A-Fa-f]{2})([,][0][x][0-9A-Fa-f]{2}){4}$")
            .WithMessage("The RXAddress mustt not be null, not empty, and must have 10 hex digits ");

        RuleFor(nrf => nrf.TXAddress)
            .NotNull()
            .NotEmpty()
            .Length(24)
            .Matches(@"^([0][x][0-9A-Fa-f]{2})([,][0][x][0-9A-Fa-f]{2}){4}$")
            .WithMessage("The TXAddress mustt not be null, not empty, and must have 10 hex digits ");

        RuleFor(nrf => nrf.CRC)
            .NotNull()
            .NotEmpty()
            .Must(x => x == 8 || x == 16)
            .WithMessage("The CRC Length must not be null, not empty and have to be 8 or 16");

        RuleFor(nrf => nrf.Rate)
            .NotNull()
            .NotEmpty()
            .Must(x => x == 250 || x == 1 || x == 2)
            .WithMessage("The Rate must not be null, not empty and have to be 1 or 2 or 250");
    }
}
