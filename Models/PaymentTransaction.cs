using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class PaymentTransaction
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    [MaxLength(32)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(160)]
    public string ProviderReference { get; set; } = string.Empty;

    public decimal AmountUsd { get; set; }

    [MaxLength(12)]
    public string Currency { get; set; } = "USD";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(2000)]
    public string RawResponse { get; set; } = string.Empty;
}
