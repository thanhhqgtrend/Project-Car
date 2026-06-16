using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class BookingConfirmationViewModel
{
    public Booking Booking { get; set; } = new();
    public bool IsPaymentConfigured { get; set; }
    public bool IsVerified { get; set; }
    public string EmailOrPhone { get; set; } = string.Empty;
}
