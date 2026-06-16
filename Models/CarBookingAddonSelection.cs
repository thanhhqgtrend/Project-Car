using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class CarBookingAddonSelection
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int CarBookingAddonId { get; set; }
    public CarBookingAddon? CarBookingAddon { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public AddonPricingMode PricingMode { get; set; } = AddonPricingMode.Fixed;

    public int Quantity { get; set; } = 1;

    public int IncludedQuantity { get; set; }

    public decimal UnitPriceUsd { get; set; }

    public decimal PriceUsd { get; set; }
}
