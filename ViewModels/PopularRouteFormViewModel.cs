using System.ComponentModel.DataAnnotations;
using System.Web;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class PopularRouteFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string PriceLabel { get; set; } = string.Empty;

    [StringLength(320)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string BookingUrl { get; set; } = "/booking/search";

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    [StringLength(220)]
    public string ImageAltText { get; set; } = string.Empty;

    public HttpPostedFileBase? ImageFile { get; set; }

    public string? CurrentImageUrl { get; set; }

    public List<MediaAsset> AvailableMedia { get; set; } = [];

    public static PopularRouteFormViewModel FromEntity(PopularRoute route) => new()
    {
        Id = route.Id,
        Title = route.Title,
        PriceLabel = route.PriceLabel,
        Description = route.Description,
        BookingUrl = route.BookingUrl,
        DisplayOrder = route.DisplayOrder,
        IsActive = route.IsActive,
        MediaAssetId = route.MediaAssetId,
        CurrentImageUrl = route.Image?.SecureUrl
    };

    public void ApplyTo(PopularRoute route)
    {
        route.Title = Title.Trim();
        route.PriceLabel = PriceLabel.Trim();
        route.Description = Description?.Trim() ?? string.Empty;
        route.BookingUrl = NormalizeBookingUrl(BookingUrl);
        route.DisplayOrder = DisplayOrder;
        route.IsActive = IsActive;
        route.MediaAssetId = MediaAssetId;
    }

    private static string NormalizeBookingUrl(string value)
    {
        var url = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/booking/search";
        }

        if (url.StartsWith("/") || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return "/" + url;
    }
}
