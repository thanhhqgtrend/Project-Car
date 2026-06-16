using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class Booking
{
    public int Id { get; set; }

    [MaxLength(24)]
    public string BookingNumber { get; set; } = string.Empty;

    public ServiceType ServiceType { get; set; } = ServiceType.Car;

    public TripType TripType { get; set; }

    public int? AirportId { get; set; }
    public Airport? Airport { get; set; }

    public int CarVehicleTypeId { get; set; }
    public CarVehicleType? CarVehicleType { get; set; }

    [MaxLength(320)]
    public string PickupAddress { get; set; } = string.Empty;

    [MaxLength(320)]
    public string DropoffAddress { get; set; } = string.Empty;

    public DateTime PickupDateTimeUtc { get; set; }

    public DateTime? HireStartDate { get; set; }

    public DateTime? HireEndDate { get; set; }

    public int HireTotalDays { get; set; } = 1;

    public int PassengerCount { get; set; }

    public decimal DistanceKm { get; set; }

    public DistanceStatus DistanceStatus { get; set; }

    public decimal EstimatedPriceUsd { get; set; }

    public decimal BasePriceUsd { get; set; }

    public decimal AddonTotalUsd { get; set; }

    public decimal DiscountUsd { get; set; }

    public decimal TaxFeeUsd { get; set; }

    public decimal TotalPriceUsd { get; set; }

    [MaxLength(12)]
    public string Currency { get; set; } = "USD";

    [MaxLength(80)]
    public string CouponCode { get; set; } = string.Empty;

    public bool TermsAccepted { get; set; }

    [MaxLength(12)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(80)]
    public string FlightNumber { get; set; } = string.Empty;

    [MaxLength(80)]
    public string MessagingApp { get; set; } = string.Empty;

    [MaxLength(80)]
    public string MessagingHandle { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAtUtc { get; set; }

    public ICollection<CarBookingAddonSelection> CarAddonSelections { get; set; } = [];
}
