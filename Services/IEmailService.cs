using LuxuryCar.Models;

namespace LuxuryCar.Services;

public interface IEmailService
{
    Task SendBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default);
    Task SendBookingPaidAsync(Booking booking, CancellationToken cancellationToken = default);
    Task SendManualBookingEmailAsync(Booking booking, string subject, string bodyHtml, CancellationToken cancellationToken = default);
    Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default);
}