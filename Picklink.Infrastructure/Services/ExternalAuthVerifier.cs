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
            "Facebook" => VerifyFacebookAsync(token, cancellationToken),
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

    private async Task<ExternalUserInfo> VerifyFacebookAsync(string accessToken, CancellationToken cancellationToken)
    {
        var appId = _options.Facebook.AppId;
        var appSecret = _options.Facebook.AppSecret;
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            throw new AppException("Facebook login is not configured.", 503);
        }

        var graphBaseUrl = _options.Facebook.GraphBaseUrl.TrimEnd('/');
        var appAccessToken = $"{appId}|{appSecret}";
        var debugEndpoint =
            $"{graphBaseUrl}/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

        using var debugResponse = await httpClient.GetAsync(debugEndpoint, cancellationToken);
        if (!debugResponse.IsSuccessStatusCode)
        {
            throw new AppException("Facebook token is invalid.", 401);
        }

        await using (var debugStream = await debugResponse.Content.ReadAsStreamAsync(cancellationToken))
        {
            var debugPayload = await JsonSerializer.DeserializeAsync<FacebookDebugTokenResponse>(debugStream, JsonOptions, cancellationToken);
            if (debugPayload?.Data is null ||
                !debugPayload.Data.IsValid ||
                !string.Equals(debugPayload.Data.AppId, appId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(debugPayload.Data.UserId))
            {
                throw new AppException("Facebook token is invalid.", 401);
            }
        }

        var profileEndpoint =
            $"{graphBaseUrl}/me?fields=id,name,email,picture.type(large)&access_token={Uri.EscapeDataString(accessToken)}";

        using var profileResponse = await httpClient.GetAsync(profileEndpoint, cancellationToken);
        if (!profileResponse.IsSuccessStatusCode)
        {
            throw new AppException("Cannot read Facebook profile.", 401);
        }

        await using var profileStream = await profileResponse.Content.ReadAsStreamAsync(cancellationToken);
        var profile = await JsonSerializer.DeserializeAsync<FacebookProfile>(profileStream, JsonOptions, cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.Id))
        {
            throw new AppException("Facebook profile is invalid.", 401);
        }

        return new ExternalUserInfo(
            "Facebook",
            profile.Id,
            profile.Email,
            !string.IsNullOrWhiteSpace(profile.Email),
            profile.Name,
            profile.Picture?.Data?.Url);
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return "Google";
        }

        if (string.Equals(provider, "Facebook", StringComparison.OrdinalIgnoreCase))
        {
            return "Facebook";
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

    private sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugToken? Data { get; set; }
    }

    private sealed class FacebookDebugToken
    {
        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }
    }

    private sealed class FacebookProfile
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }

    private sealed class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; set; }
    }

    private sealed class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
