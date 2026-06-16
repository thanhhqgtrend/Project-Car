using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminBookingListViewModel
{
    public List<Booking> Bookings { get; set; } = [];
    public BookingStatus? Status { get; set; }
    public string Search { get; set; } = string.Empty;
}
