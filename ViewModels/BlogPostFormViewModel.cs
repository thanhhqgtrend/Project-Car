using System.ComponentModel.DataAnnotations;
using System.Web;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class BlogPostFormViewModel
{
    public int Id { get; set; }

    [StringLength(12)]
    public string Culture { get; set; } = "en";

    [Required, StringLength(140)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Excerpt { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    [StringLength(180)]
    public string MetaTitle { get; set; } = string.Empty;

    [StringLength(320)]
    public string MetaDescription { get; set; } = string.Empty;

    [StringLength(320)]
    public string CanonicalUrl { get; set; } = string.Empty;

    [StringLength(80)]
    public string Robots { get; set; } = "index,follow";

    [StringLength(180)]
    public string OgTitle { get; set; } = string.Empty;

    [StringLength(320)]
    public string OgDescription { get; set; } = string.Empty;

    public int? FeaturedMediaAssetId { get; set; }

    public int? OgMediaAssetId { get; set; }

    public HttpPostedFileBase? FeaturedImageFile { get; set; }

    [StringLength(220)]
    public string FeaturedImageAltText { get; set; } = string.Empty;

    public HttpPostedFileBase? OgImageFile { get; set; }

    [StringLength(220)]
    public string OgImageAltText { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? CurrentFeaturedImageUrl { get; set; }

    public string? CurrentOgImageUrl { get; set; }

    public List<MediaAsset> AvailableMedia { get; set; } = [];

    public IReadOnlyList<string> SupportedCultures { get; set; } = ["en", "vi", "ko", "zh", "ja", "fr"];

    public static BlogPostFormViewModel FromEntity(BlogPost post, string culture)
    {
        var translation = post.Translations.FirstOrDefault(x => x.Culture == culture)
            ?? post.Translations.FirstOrDefault(x => x.Culture == "en")
            ?? post.Translations.FirstOrDefault();

        return new BlogPostFormViewModel
        {
            Id = post.Id,
            Culture = culture,
            Slug = translation?.Slug ?? string.Empty,
            Title = translation?.Title ?? string.Empty,
            Excerpt = translation?.Excerpt ?? string.Empty,
            BodyHtml = translation?.BodyHtml ?? string.Empty,
            MetaTitle = string.IsNullOrWhiteSpace(translation?.MetaTitle) ? post.MetaTitle : translation.MetaTitle,
            MetaDescription = string.IsNullOrWhiteSpace(translation?.MetaDescription) ? post.MetaDescription : translation.MetaDescription,
            CanonicalUrl = string.IsNullOrWhiteSpace(translation?.CanonicalUrl) ? post.CanonicalUrl : translation.CanonicalUrl,
            Robots = string.IsNullOrWhiteSpace(translation?.Robots) ? post.Robots : translation.Robots,
            OgTitle = string.IsNullOrWhiteSpace(translation?.OgTitle) ? post.OgTitle : translation.OgTitle,
            OgDescription = string.IsNullOrWhiteSpace(translation?.OgDescription) ? post.OgDescription : translation.OgDescription,
            FeaturedMediaAssetId = post.FeaturedMediaAssetId,
            OgMediaAssetId = translation?.OgMediaAssetId ?? post.OgMediaAssetId,
            IsPublished = post.IsPublished,
            PublishedAt = post.PublishedAtUtc?.ToLocalTime(),
            CurrentFeaturedImageUrl = post.FeaturedMediaAsset?.SecureUrl,
            CurrentOgImageUrl = translation?.OgMediaAsset?.SecureUrl ?? post.OgMediaAsset?.SecureUrl,
            FeaturedImageAltText = post.FeaturedMediaAsset?.AltText ?? string.Empty,
            OgImageAltText = translation?.OgMediaAsset?.AltText ?? post.OgMediaAsset?.AltText ?? string.Empty
        };
    }
}
