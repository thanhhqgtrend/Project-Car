using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalBookings { get; set; }
    public int PendingPaymentBookings { get; set; }
    public int PaidBookings { get; set; }
    public int TodayBookings { get; set; }
    public decimal EstimatedRevenueUsd { get; set; }
    public List<Booking> RecentBookings { get; set; } = [];
    public Dictionary<BookingStatus, int> StatusCounts { get; set; } = [];
}
