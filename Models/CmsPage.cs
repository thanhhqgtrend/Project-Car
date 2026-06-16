using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class CmsPage
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(240)]
    public string PublicPath { get; set; } = string.Empty;

    public string OldPaths { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string MetaDescription { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAtUtc { get; set; }

    public int? FeaturedMediaAssetId { get; set; }

    public MediaAsset? FeaturedMediaAsset { get; set; }

    [MaxLength(180)]
    public string MetaTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string CanonicalUrl { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Robots { get; set; } = "index,follow";

    [MaxLength(180)]
    public string OgTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string OgDescription { get; set; } = string.Empty;

    public int? OgMediaAssetId { get; set; }

    public MediaAsset? OgMediaAsset { get; set; }

    public List<CmsPageTranslation> Translations { get; set; } = [];
}
