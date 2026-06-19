using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Application.Interfaces;

namespace Picklink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserService currentUserService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator,
    IValidator<LogoutRequest> logoutValidator,
    IValidator<ExternalLoginRequest> externalLoginValidator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(registerValidator, request, cancellationToken);
        var result = await authService.RegisterAsync(request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registered successfully."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(loginValidator, request, cancellationToken);
        var result = await authService.LoginAsync(request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Logged in successfully."));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(refreshTokenValidator, request, cancellationToken);
        var result = await authService.RefreshTokenAsync(request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed."));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(logoutValidator, request, cancellationToken);
        await authService.LogoutAsync(request, GetIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new AppException("Unauthorized.", 401);
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Google(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(externalLoginValidator, request, cancellationToken);
        var result = await authService.ExternalLoginAsync("Google", request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Logged in with Google."));
    }

    [HttpPost("facebook")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Facebook(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(externalLoginValidator, request, cancellationToken);
        var result = await authService.ExternalLoginAsync("Facebook", request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Logged in with Facebook."));
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetDeviceInfo() => Request.Headers.UserAgent.ToString();

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(error => error.ErrorMessage).ToArray());

            throw new AppException("Validation failed.", 400, errors);
        }
    }
}
