namespace LuxuryCar.Models;

public enum BookingStatus
{
    PendingPayment = 0,
    Paid = 1,
    PaymentFailed = 2,
    PendingContact = 3,
    Confirmed = 4,
    Cancelled = 5
}
