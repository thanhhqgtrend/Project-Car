using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class BlogPostTranslation
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    [MaxLength(12)]
    public string Culture { get; set; } = "en";

    [MaxLength(140)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Excerpt { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    [MaxLength(180)]
    public string MetaTitle { get; set; } = string.Empty;

    [MaxLength(320)]
    public string MetaDescription { get; set; } = string.Empty;

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
