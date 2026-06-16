using System.ComponentModel.DataAnnotations;
using System.Web;
using LuxuryCar.Models;
using System.Web.Mvc;

namespace LuxuryCar.ViewModels;

public class BookingSearchViewModel
{
    public bool SearchSubmitted { get; set; }

    [Required]
    public TripType TripType { get; set; } = TripType.AirportPickup;

    public int? AirportId { get; set; }

    [StringLength(320)]
    public string? OriginAddress { get; set; } = string.Empty;

    [StringLength(320)]
    public string? DestinationAddress { get; set; } = string.Empty;

    [Required]
    public DateTime PickupDateTime { get; set; } = DateTime.Now.AddHours(4);

    [StringLength(80)]
    public string? FlightNumber { get; set; } = string.Empty;

    [Range(1, 20)]
    public int PassengerCount { get; set; } = 1;

    public DateTime StartDate { get; set; } = DateTime.Now.AddDays(1).Date;
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(2).Date;

    public int HireTotalDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days);

    public List<SelectListItem> Airports { get; set; } = [];

    public List<CarVehicleQuoteOption> CarVehicleOptions { get; set; } = [];

    public string GeoapifyApiKey { get; set; } = string.Empty;
}

public class CarVehicleQuoteOption
{
    public CarVehicleType CarVehicle { get; set; } = new();
    public decimal DistanceKm { get; set; }
    public DistanceStatus DistanceStatus { get; set; }
    public decimal BasePriceUsd { get; set; }
    public decimal TaxFeeUsd { get; set; }
    public decimal TotalPriceUsd { get; set; }
}

public class BookingResultsViewModel : BookingSearchViewModel
{
}

public class BookingCheckoutViewModel : BookingSearchViewModel
{
    [Required]
    public int CarVehicleTypeId { get; set; }

    public CarVehicleType? CarVehicle { get; set; }

    public decimal DistanceKm { get; set; }
    public DistanceStatus DistanceStatus { get; set; }
    public decimal BasePriceUsd { get; set; }
    public decimal AddonTotalUsd { get; set; }
    public decimal DiscountUsd { get; set; }
    public decimal TaxFeeUsd { get; set; }
    public decimal TotalPriceUsd { get; set; }

    [Required, StringLength(12)]
    public string Title { get; set; } = "Mr";

    [StringLength(160)]
    public string? FullName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(60)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(12)]
    public string CountryCode { get; set; } = "+84";

    [Required, StringLength(80)]
    public string MessagingApp { get; set; } = "WhatsApp";

    [StringLength(80)]
    public string? MessagingHandle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; } = string.Empty;

    [Required, StringLength(5)]
    public string PickupTime { get; set; } = string.Empty;

    [StringLength(80)]
    public string? CouponCode { get; set; } = string.Empty;

    public bool TermsAccepted { get; set; }

    public List<int> SelectedAddonIds { get; set; } = [];

    public Dictionary<int, int> AddonQuantities { get; set; } = [];

    public List<CarBookingAddon> Addons { get; set; } = [];
}

public class PaymentPageViewModel
{
    public Booking Booking { get; set; } = new();
    public bool IsPaymentConfigured { get; set; }
    public bool HasPayPal { get; set; }
    public bool HasStripe { get; set; }
}

public class CarBookingAddonFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(320)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 99999)]
    public decimal PriceUsd { get; set; }

    public AddonPricingMode PricingMode { get; set; } = AddonPricingMode.Fixed;

    [Range(0, 9999)]
    public int IncludedQuantity { get; set; }

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    [StringLength(220)]
    public string ImageAltText { get; set; } = string.Empty;

    public HttpPostedFileBase? ImageFile { get; set; }

    public string? CurrentImageUrl { get; set; }

    public List<MediaAsset> AvailableMedia { get; set; } = [];

    public static CarBookingAddonFormViewModel FromEntity(CarBookingAddon addon) => new()
    {
        Id = addon.Id,
        Name = addon.Name,
        Description = addon.Description,
        PriceUsd = addon.PriceUsd,
        PricingMode = addon.PricingMode,
        IncludedQuantity = addon.IncludedQuantity,
        DisplayOrder = addon.DisplayOrder,
        IsActive = addon.IsActive,
        MediaAssetId = addon.MediaAssetId,
        CurrentImageUrl = addon.Image?.SecureUrl
    };

    public void ApplyTo(CarBookingAddon addon)
    {
        addon.Name = Name.Trim();
        addon.Description = Description?.Trim() ?? string.Empty;
        addon.PriceUsd = PriceUsd;
        addon.PricingMode = PricingMode;
        addon.IncludedQuantity = Math.Max(0, IncludedQuantity);
        addon.DisplayOrder = DisplayOrder;
        addon.IsActive = IsActive;
        addon.MediaAssetId = MediaAssetId;
    }
}
