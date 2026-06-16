using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class MediaAsset
{
    public int Id { get; set; }

    [MaxLength(220)]
    public string PublicId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string SecureUrl { get; set; } = string.Empty;

    [MaxLength(220)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long Bytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    [MaxLength(220)]
    public string AltText { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Folder { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAtUtc { get; set; }
}
