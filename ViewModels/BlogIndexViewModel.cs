using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class BlogIndexViewModel
{
    public List<BlogPostViewModel> Posts { get; set; } = [];
    public List<BlogPostViewModel> FeaturedPosts { get; set; } = [];
    public string Search { get; set; } = string.Empty;
}