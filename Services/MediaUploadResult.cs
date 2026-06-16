namespace LuxuryCar.Services;

public record MediaUploadResult(
    string PublicId,
    string Url,
    string SecureUrl,
    string FileName,
    string ContentType,
    long Bytes,
    int Width,
    int Height,
    string Folder);
