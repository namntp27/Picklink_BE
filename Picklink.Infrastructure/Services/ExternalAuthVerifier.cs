using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Picklink.Application.Common;
using Picklink.Application.Interfaces;
using Picklink.Infrastructure.Options;

namespace Picklink.Infrastructure.Services;

public sealed class ExternalAuthVerifier(HttpClient httpClient, IOptions<ExternalAuthOptions> options) : IExternalAuthVerifier
{
    private readonly ExternalAuthOptions _options = options.Value;

    public Task<ExternalUserInfo> VerifyAsync(string provider, string token, CancellationToken cancellationToken = default)
    {
        return NormalizeProvider(provider) switch
        {
            "Google" => VerifyGoogleAsync(token, cancellationToken),
            _ => throw new AppException("Unsupported external login provider.", 400)
        };
    }

    private async Task<ExternalUserInfo> VerifyGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = _options.Google.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new AppException("Google login is not configured.", 503);
        }

        var endpoint = $"{_options.Google.TokenInfoEndpoint}?id_token={Uri.EscapeDataString(idToken)}";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AppException("Google token is invalid.", 401);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GoogleTokenInfo>(stream, JsonOptions, cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new AppException("Google token is invalid.", 401);
        }

        if (!string.Equals(payload.Audience, clientId, StringComparison.Ordinal))
        {
            throw new AppException("Google token audience is invalid.", 401);
        }

        if (!string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Google email is not verified.", 401);
        }

        return new ExternalUserInfo(
            "Google",
            payload.Subject,
            payload.Email,
            true,
            payload.Name,
            payload.Picture);
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return "Google";
        }

        return provider;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("aud")]
        public string? Audience { get; set; }

        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

}
