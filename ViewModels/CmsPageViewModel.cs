namespace LuxuryCar.ViewModels;

public class CmsPageViewModel
{
    public LuxuryCar.Models.CmsPage Page { get; set; } = new();

    public LuxuryCar.Models.CmsPageTranslation Translation { get; set; } = new();

    public string Slug { get; set; } = string.Empty;

    public string PublicPath { get; set; } = string.Empty;

    public string Culture { get; set; } = "en";

    public string Title { get; set; } = string.Empty;

    public string MetaTitle { get; set; } = string.Empty;

    public string MetaDescription { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string Robots { get; set; } = "index,follow";

    public string OgTitle { get; set; } = string.Empty;

    public string OgDescription { get; set; } = string.Empty;

    public string OgImageUrl { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;
}
