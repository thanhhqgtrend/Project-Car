using System.Net;
using System.Net.Mail;
using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Models;

namespace LuxuryCar.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _db;
    private readonly IAppSettingService _settings;
    private readonly IAppLogger<EmailService> _logger;

    public EmailService(ApplicationDbContext db, IAppSettingService settings, IAppLogger<EmailService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    public Task SendBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default) =>
        SendAsync(booking, $"Booking {booking.BookingNumber} received", $"Thank you {booking.FullName}, your booking {booking.BookingNumber} has been received. Estimated fare: ${booking.EstimatedPriceUsd:0.00}.", cancellationToken);

    public Task SendBookingPaidAsync(Booking booking, CancellationToken cancellationToken = default) =>
        SendAsync(booking, $"Booking {booking.BookingNumber} paid", $"Your payment for booking {booking.BookingNumber} was received. We will confirm your driver details soon.", cancellationToken);

    public Task SendManualBookingEmailAsync(Booking booking, string subject, string bodyHtml, CancellationToken cancellationToken = default) =>
        SendAsync(booking, subject, bodyHtml, cancellationToken, isBodyHtml: true);

    public async Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        var brandName = await _settings.GetAsync("Site:BrandName", "Vietnam Transfer", cancellationToken);
        await SendAsync(null, toEmail, $"{brandName} SMTP test", $"<p>This is a test email from the {brandName} admin settings page.</p>", cancellationToken, isBodyHtml: true, throwOnFailure: true);
    }

    private async Task SendAsync(Booking booking, string subject, string body, CancellationToken cancellationToken, bool isBodyHtml = false)
    {
        await SendAsync(booking, booking.Email, subject, body, cancellationToken, isBodyHtml);
    }

    private async Task SendAsync(Booking? booking, string toEmail, string subject, string body, CancellationToken cancellationToken, bool isBodyHtml = false, bool throwOnFailure = false)
    {
        var log = new EmailLog { BookingId = booking?.Id, ToEmail = toEmail, Subject = subject };

        try
        {
            var host = await _settings.GetAsync("Email:SmtpHost", cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogInformation($"SMTP not configured. Email to {toEmail}: {subject}");
                log.ErrorMessage = "SMTP not configured; logged only.";
                throw new InvalidOperationException("SMTP is not configured.");
            }
            else
            {
                var portValue = await _settings.GetAsync("Email:SmtpPort", "587", cancellationToken);
                var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : 587;
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = await _settings.GetBoolAsync("Email:EnableSsl", true, cancellationToken)
                };
                var user = await _settings.GetAsync("Email:Username", cancellationToken: cancellationToken);
                var pass = await _settings.GetAsync("Email:Password", cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(user))
                {
                    client.Credentials = new NetworkCredential(user, pass);
                }

                var from = await _settings.GetAsync("Email:From", "bookings@vietnamtransfer.local", cancellationToken);
                using var message = new MailMessage(from, toEmail, subject, body)
                {
                    IsBodyHtml = isBodyHtml
                };
                await client.SendMailAsync(message);
                log.IsSuccess = true;
            }
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Failed to send booking email.");
            if (throwOnFailure)
            {
                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}