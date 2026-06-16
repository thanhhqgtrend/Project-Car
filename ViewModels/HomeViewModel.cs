using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class HomeViewModel
{
    public List<Airport> Airports { get; set; } = [];
    public List<CarVehicleType> CarVehicleTypes { get; set; } = [];
    public List<CarBookingAddon> FeaturedAddons { get; set; } = [];
    public List<PopularRoute> PopularRoutes { get; set; } = [];
    public List<Airport> PopularDestinations { get; set; } = [];
    public List<CmsPage> Articles { get; set; } = [];
    public List<BlogPostViewModel> LatestBlogPosts { get; set; } = [];
    public string GeoapifyApiKey { get; set; } = string.Empty;
}
