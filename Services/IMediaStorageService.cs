using System.Web;

namespace LuxuryCar.Services;

public interface IMediaStorageService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<MediaUploadResult> UploadImageAsync(HttpPostedFileBase file, string altText, CancellationToken cancellationToken = default);
    Task<MediaSyncResult> SyncImagesAsync(CancellationToken cancellationToken = default);
    Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
