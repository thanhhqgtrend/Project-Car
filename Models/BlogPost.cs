using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class BlogPost
{
    public int Id { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAtUtc { get; set; }

    public int? FeaturedMediaAssetId { get; set; }

    public MediaAsset? FeaturedMediaAsset { get; set; }

    [MaxLength(180)]
    public string MetaTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string MetaDescription { get; set; } = string.Empty;

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

    public List<BlogPostTranslation> Translations { get; set; } = [];
}
