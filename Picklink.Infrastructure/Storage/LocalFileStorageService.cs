using Microsoft.Extensions.Options;
using Picklink.Application.Common;
using Picklink.Application.Interfaces;
using Picklink.Infrastructure.Options;

namespace Picklink.Infrastructure.Storage;

public sealed class LocalFileStorageService(IOptions<StorageOptions> options) : IFileStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf"
    };

    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new AppException("File type is not allowed.", 400);
        }

        if (stream.Length > _options.MaxFileBytes)
        {
            throw new AppException("File is too large.", 400);
        }

        var extension = Path.GetExtension(fileName);
        var safeFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
        var root = Path.GetFullPath(_options.UploadRoot);
        Directory.CreateDirectory(root);

        var path = Path.Combine(root, safeFileName);
        await using var output = File.Create(path);
        stream.Position = 0;
        await stream.CopyToAsync(output, cancellationToken);

        return $"{_options.PublicBasePath.TrimEnd('/')}/{safeFileName}";
    }
}
