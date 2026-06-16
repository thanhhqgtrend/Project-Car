using System.ComponentModel.DataAnnotations;
using System.Web;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class CarVehicleTypeFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(160)]
    public string Description { get; set; } = string.Empty;

    [StringLength(80)]
    public string Brand { get; set; } = string.Empty;

    [Range(1, 20)]
    public int PassengerCapacity { get; set; } = 4;

    [Range(0, 20)]
    public int LuggageCapacity { get; set; } = 2;

    [Range(0, 99999)]
    public decimal BaseFareUsd { get; set; }

    [Range(0, 99999)]
    public decimal PricePerKmUsd { get; set; }

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public bool IsLuxury { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    [StringLength(220)]
    public string ImageAltText { get; set; } = string.Empty;

    public HttpPostedFileBase? ImageFile { get; set; }

    public string? CurrentImageUrl { get; set; }

    public List<MediaAsset> AvailableMedia { get; set; } = [];

    public static CarVehicleTypeFormViewModel FromEntity(CarVehicleType vehicle) => new()
    {
        Id = vehicle.Id,
        Name = vehicle.Name,
        Description = vehicle.Description,
        Brand = vehicle.Brand,
        PassengerCapacity = vehicle.PassengerCapacity,
        LuggageCapacity = vehicle.LuggageCapacity,
        BaseFareUsd = vehicle.BaseFareUsd,
        PricePerKmUsd = vehicle.PricePerKmUsd,
        DisplayOrder = vehicle.DisplayOrder,
        IsLuxury = vehicle.IsLuxury,
        IsActive = vehicle.IsActive,
        MediaAssetId = vehicle.MediaAssetId,
        CurrentImageUrl = vehicle.Image?.SecureUrl
    };

    public void ApplyTo(CarVehicleType vehicle)
    {
        vehicle.Name = Name.Trim();
        vehicle.Description = Description?.Trim() ?? string.Empty;
        vehicle.Brand = Brand?.Trim() ?? string.Empty;
        vehicle.PassengerCapacity = PassengerCapacity;
        vehicle.LuggageCapacity = LuggageCapacity;
        vehicle.BaseFareUsd = BaseFareUsd;
        vehicle.PricePerKmUsd = PricePerKmUsd;
        vehicle.DisplayOrder = DisplayOrder;
        vehicle.IsLuxury = IsLuxury;
        vehicle.IsActive = IsActive;
        vehicle.MediaAssetId = MediaAssetId;
    }
}
