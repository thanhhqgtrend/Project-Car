using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminMediaListViewModel
{
    public List<MediaAsset> Assets { get; set; } = [];
    public string Search { get; set; } = string.Empty;
    public bool IsCloudinaryConfigured { get; set; }
}
