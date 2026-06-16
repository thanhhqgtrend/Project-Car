using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class CmsPageTranslation
{
    public int Id { get; set; }

    public int CmsPageId { get; set; }

    public CmsPage CmsPage { get; set; } = null!;

    [MaxLength(12)]
    public string Culture { get; set; } = "en";

    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string MetaDescription { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    [MaxLength(180)]
    public string MetaTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string CanonicalUrl { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Robots { get; set; } = string.Empty;

    [MaxLength(180)]
    public string OgTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string OgDescription { get; set; } = string.Empty;

    public int? OgMediaAssetId { get; set; }

    public MediaAsset? OgMediaAsset { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
