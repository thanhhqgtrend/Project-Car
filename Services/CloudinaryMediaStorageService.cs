using CloudinaryDotNet;
using System.Web;
using CloudinaryDotNet.Actions;
using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Models;
using System.Data.Entity;

namespace LuxuryCar.Services;

public class CloudinaryMediaStorageService : IMediaStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const long MaxFileBytes = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly IAppSettingService _settings;
    private readonly IAppLogger<CloudinaryMediaStorageService> _logger;

    public CloudinaryMediaStorageService(ApplicationDbContext db, IAppSettingService settings, IAppLogger<CloudinaryMediaStorageService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var settings = await ResolveSettingsAsync(cancellationToken);
        return settings.IsConfigured;
    }

    public async Task<MediaUploadResult> UploadImageAsync(HttpPostedFileBase file, string altText, CancellationToken cancellationToken = default)
    {
        ValidateImage(file);
        var settings = await ResolveSettingsAsync(cancellationToken);
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Cloudinary is not configured. Please set Cloudinary:CloudName, Cloudinary:ApiKey and Cloudinary:ApiSecret.");
        }

        var folder = settings.Folder;
        var cloudinary = CreateClient(settings);
        var stream = file.InputStream;
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
            Context = string.IsNullOrWhiteSpace(altText) ? null : new StringDictionary("alt", altText)
        };

        var result = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            _logger.LogWarning(new InvalidOperationException(result.Error.Message), "Cloudinary upload failed.");
            throw new InvalidOperationException(result.Error.Message);
        }

        return new MediaUploadResult(
            result.PublicId,
            result.Url?.ToString() ?? string.Empty,
            result.SecureUrl?.ToString() ?? string.Empty,
            file.FileName,
            file.ContentType,
            file.ContentLength,
            result.Width,
            result.Height,
            folder);
    }

    public async Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var settings = await ResolveSettingsAsync(cancellationToken);
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        var result = await CreateClient(settings).DestroyAsync(new DeletionParams(publicId));
        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
    }

    public async Task<MediaSyncResult> SyncImagesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await ResolveSettingsAsync(cancellationToken);
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        var cloudinary = CreateClient(settings);
        var imported = 0;
        var skipped = 0;
        string? nextCursor = null;
        var knownPublicIds = new HashSet<string>(await _db.MediaAssets.Select(x => x.PublicId).ToListAsync(cancellationToken));

        do
        {
            var result = await cloudinary.ListResourcesAsync(new ListResourcesParams
            {
                ResourceType = ResourceType.Image,
                Type = "upload",
                MaxResults = 100,
                NextCursor = nextCursor
            });

            foreach (var resource in result.Resources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var folderPrefix = string.IsNullOrWhiteSpace(settings.Folder) ? string.Empty : settings.Folder.Trim('/') + "/";
                if (!string.IsNullOrWhiteSpace(folderPrefix) && !resource.PublicId.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!knownPublicIds.Add(resource.PublicId))
                {
                    skipped++;
                    continue;
                }

                var fileName = Path.GetFileName(resource.PublicId);
                if (!string.IsNullOrWhiteSpace(resource.Format) && !fileName.EndsWith($".{resource.Format}", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{fileName}.{resource.Format}";
                }

                _db.MediaAssets.Add(new MediaAsset
                {
                    PublicId = resource.PublicId,
                    Url = resource.Url?.ToString() ?? string.Empty,
                    SecureUrl = resource.SecureUrl?.ToString() ?? string.Empty,
                    FileName = string.IsNullOrWhiteSpace(fileName) ? resource.PublicId : fileName,
                    ContentType = string.IsNullOrWhiteSpace(resource.Format) ? "image" : $"image/{resource.Format}",
                    Bytes = resource.Bytes,
                    Width = resource.Width,
                    Height = resource.Height,
                    AltText = fileName,
                    Folder = settings.Folder,
                    CreatedAtUtc = DateTime.TryParse(resource.CreatedAt, out var createdAt) ? createdAt : DateTime.UtcNow
                });
                imported++;
            }

            await _db.SaveChangesAsync(cancellationToken);
            nextCursor = result.NextCursor;
        }
        while (!string.IsNullOrWhiteSpace(nextCursor));

        return new MediaSyncResult(imported, skipped);
    }

    private Cloudinary CreateClient(CloudinarySettings settings)
    {
        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);
        return new Cloudinary(account);
    }

    private async Task<CloudinarySettings> ResolveSettingsAsync(CancellationToken cancellationToken)
    {
        return new CloudinarySettings(
            await _settings.GetAsync("Cloudinary:CloudName", cancellationToken: cancellationToken),
            await _settings.GetAsync("Cloudinary:ApiKey", cancellationToken: cancellationToken),
            await _settings.GetAsync("Cloudinary:ApiSecret", cancellationToken: cancellationToken),
            await _settings.GetAsync("Cloudinary:Folder", "vietnamtransfer", cancellationToken));
    }

    private sealed record CloudinarySettings(string CloudName, string ApiKey, string ApiSecret, string Folder)
    {
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(CloudName) &&
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(ApiSecret);
    }

    private static void ValidateImage(HttpPostedFileBase file)
    {
        if (file.ContentLength == 0)
        {
            throw new InvalidOperationException("Please choose an image file.");
        }

        if (file.ContentLength > MaxFileBytes)
        {
            throw new InvalidOperationException("Image must be 5MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG and WebP images are allowed.");
        }
    }
}
