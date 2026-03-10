using FastEndpoints;
using FluentValidation;

namespace AleTrack.Features.Users.Commands.Logout;

/// <summary>
/// Validator for the logout request
/// </summary>
public sealed class LogoutValidator : Validator<LogoutRequest>
{
    public LogoutValidator()
    {
        RuleFor(x => x.Data)
            .NotNull();

        RuleFor(x => x.Data.RefreshToken)
            .NotEmpty()
            .When(x => x.Data is not null);
    }
}
