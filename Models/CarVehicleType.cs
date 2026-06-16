using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class CarVehicleType
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Brand { get; set; } = string.Empty;

    public int PassengerCapacity { get; set; }

    public int LuggageCapacity { get; set; }

    public decimal BaseFareUsd { get; set; }

    public decimal PricePerKmUsd { get; set; }

    public decimal DailyRateUsd { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsLuxury { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    public MediaAsset? Image { get; set; }
}