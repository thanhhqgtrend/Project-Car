using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminRedirectListViewModel
{
    public List<RedirectRule> Redirects { get; set; } = [];

    public string Status { get; set; } = "active";

    public string Search { get; set; } = string.Empty;
}
