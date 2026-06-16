using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminCmsPageListViewModel
{
    public List<CmsPage> Pages { get; set; } = [];

    public string Status { get; set; } = "all";

    public string Culture { get; set; } = "en";

    public string Search { get; set; } = string.Empty;

    public string Sort { get; set; } = "updated";
}
