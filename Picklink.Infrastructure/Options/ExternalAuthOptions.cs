namespace Picklink.Infrastructure.Options;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    public GoogleAuthOptions Google { get; set; } = new();
    public FacebookAuthOptions Facebook { get; set; } = new();
}

public sealed class GoogleAuthOptions
{
    public string? ClientId { get; set; }
    public string TokenInfoEndpoint { get; set; } = "https://oauth2.googleapis.com/tokeninfo";
}

public sealed class FacebookAuthOptions
{
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string GraphBaseUrl { get; set; } = "https://graph.facebook.com";
}
