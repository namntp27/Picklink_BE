using FluentValidation;
using Picklink.Application.DTOs;
using Picklink.Domain.Constants;

namespace Picklink.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role =>
                string.Equals(role, AppRoles.Player, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only Player or Owner can self-register.");
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
    }
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
    }
}
