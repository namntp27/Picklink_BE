namespace Picklink.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadRoot { get; set; } = "wwwroot/uploads";
    public string PublicBasePath { get; set; } = "/uploads";
    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;
}
