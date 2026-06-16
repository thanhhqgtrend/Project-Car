using System.ComponentModel.DataAnnotations;
using System.Web;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class CmsPageFormViewModel
{
    public int Id { get; set; }

    [StringLength(12)]
    public string Culture { get; set; } = "en";

    [Required, StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string PublicPath { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

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

    public bool IsPublished { get; set; } = true;

    public string? CurrentFeaturedImageUrl { get; set; }

    public string? CurrentOgImageUrl { get; set; }

    public List<MediaAsset> AvailableMedia { get; set; } = [];

    public IReadOnlyList<string> SupportedCultures { get; set; } = ["en", "vi", "ko", "zh", "ja", "fr"];

    public static CmsPageFormViewModel FromEntity(CmsPage page, string culture)
    {
        var translation = page.Translations.FirstOrDefault(x => x.Culture == culture)
            ?? page.Translations.FirstOrDefault(x => x.Culture == "en")
            ?? page.Translations.FirstOrDefault();

        return new CmsPageFormViewModel
        {
            Id = page.Id,
            Culture = culture,
            Slug = page.Slug,
            PublicPath = page.PublicPath,
            Title = string.IsNullOrWhiteSpace(translation?.Title) ? page.Title : translation.Title,
            BodyHtml = string.IsNullOrWhiteSpace(translation?.BodyHtml) ? page.BodyHtml : translation.BodyHtml,
            MetaTitle = string.IsNullOrWhiteSpace(translation?.MetaTitle) ? page.MetaTitle : translation.MetaTitle,
            MetaDescription = string.IsNullOrWhiteSpace(translation?.MetaDescription) ? page.MetaDescription : translation.MetaDescription,
            CanonicalUrl = string.IsNullOrWhiteSpace(translation?.CanonicalUrl) ? page.CanonicalUrl : translation.CanonicalUrl,
            Robots = string.IsNullOrWhiteSpace(translation?.Robots) ? page.Robots : translation.Robots,
            OgTitle = string.IsNullOrWhiteSpace(translation?.OgTitle) ? page.OgTitle : translation.OgTitle,
            OgDescription = string.IsNullOrWhiteSpace(translation?.OgDescription) ? page.OgDescription : translation.OgDescription,
            FeaturedMediaAssetId = page.FeaturedMediaAssetId,
            OgMediaAssetId = translation?.OgMediaAssetId ?? page.OgMediaAssetId,
            IsPublished = page.IsPublished,
            CurrentFeaturedImageUrl = page.FeaturedMediaAsset?.SecureUrl,
            CurrentOgImageUrl = translation?.OgMediaAsset?.SecureUrl ?? page.OgMediaAsset?.SecureUrl
        };
    }
}
