using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class EmailLog
{
    public int Id { get; set; }

    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    [MaxLength(180)]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(220)]
    public string Subject { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    [MaxLength(1200)]
    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
