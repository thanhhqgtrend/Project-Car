using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class BlogPostViewModel
{
    public BlogPost Post { get; set; } = new();
    public BlogPostTranslation Translation { get; set; } = new();
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string Robots { get; set; } = "index,follow";
    public string OgTitle { get; set; } = string.Empty;
    public string OgDescription { get; set; } = string.Empty;
    public string OgImageUrl { get; set; } = string.Empty;
}

public class BlogIndexViewModel
{
    public List<BlogPostViewModel> Posts { get; set; } = [];
}
