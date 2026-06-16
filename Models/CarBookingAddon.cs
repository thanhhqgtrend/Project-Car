using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class CarBookingAddon
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Description { get; set; } = string.Empty;

    public decimal PriceUsd { get; set; }

    public AddonPricingMode PricingMode { get; set; } = AddonPricingMode.Fixed;

    public int IncludedQuantity { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    public MediaAsset? Image { get; set; }
}
