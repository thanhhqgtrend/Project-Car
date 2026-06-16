using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminBlogListViewModel
{
    public List<BlogPost> Posts { get; set; } = [];
    public string Status { get; set; } = "all";
    public string Culture { get; set; } = "en";
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
}
