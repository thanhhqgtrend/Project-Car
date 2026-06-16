using System.ComponentModel.DataAnnotations;
using LuxuryCar.Models;
using System.Web.Mvc;

namespace LuxuryCar.ViewModels;

public class AdminBookingEditViewModel
{
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;

    public TripType TripType { get; set; }
    public BookingStatus Status { get; set; }

    public int? AirportId { get; set; }

    [Required]
    public int CarVehicleTypeId { get; set; }

    [Required, StringLength(320)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string DropoffAddress { get; set; } = string.Empty;

    [Required]
    public DateTime PickupDateTime { get; set; }

    [Range(1, 50)]
    public int PassengerCount { get; set; }

    [Range(0, 99999)]
    public decimal DistanceKm { get; set; }

    public DistanceStatus DistanceStatus { get; set; }

    [StringLength(12)]
    public string? Title { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(60)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(80)]
    public string? FlightNumber { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MessagingApp { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MessagingHandle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; } = string.Empty;

    [StringLength(80)]
    public string? CouponCode { get; set; } = string.Empty;

    [Range(0, 999999)]
    public decimal BasePriceUsd { get; set; }

    [Range(0, 999999)]
    public decimal AddonTotalUsd { get; set; }

    [Range(0, 999999)]
    public decimal DiscountUsd { get; set; }

    [Range(0, 999999)]
    public decimal TaxFeeUsd { get; set; }

    [Range(0, 999999)]
    public decimal TotalPriceUsd { get; set; }

    public List<SelectListItem> Airports { get; set; } = [];
    public List<SelectListItem> Vehicles { get; set; } = [];

    public static AdminBookingEditViewModel FromBooking(Booking booking, DateTime pickupDateTime) => new()
    {
        Id = booking.Id,
        BookingNumber = booking.BookingNumber,
        TripType = booking.TripType,
        Status = booking.Status,
        AirportId = booking.AirportId,
        CarVehicleTypeId = booking.CarVehicleTypeId,
        PickupAddress = booking.PickupAddress,
        DropoffAddress = booking.DropoffAddress,
        PickupDateTime = pickupDateTime,
        PassengerCount = booking.PassengerCount,
        DistanceKm = booking.DistanceKm,
        DistanceStatus = booking.DistanceStatus,
        Title = booking.Title,
        FullName = booking.FullName,
        Email = booking.Email,
        Phone = booking.Phone,
        FlightNumber = booking.FlightNumber,
        MessagingApp = booking.MessagingApp,
        MessagingHandle = booking.MessagingHandle,
        Notes = booking.Notes,
        CouponCode = booking.CouponCode,
        BasePriceUsd = booking.BasePriceUsd,
        AddonTotalUsd = booking.AddonTotalUsd,
        DiscountUsd = booking.DiscountUsd,
        TaxFeeUsd = booking.TaxFeeUsd,
        TotalPriceUsd = booking.TotalPriceUsd
    };
}
