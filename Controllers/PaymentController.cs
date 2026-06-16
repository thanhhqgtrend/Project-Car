using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Models;
using LuxuryCar.Services;
using LuxuryCar.ViewModels;
using System.Web.Mvc;
using System.Data.Entity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Routing;
using System.Text;
using System.Text.Json;

namespace LuxuryCar.Controllers;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IAppSettingService _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentController(
        ApplicationDbContext db,
        IEmailService emailService,
        IAppSettingService settings,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    [Route("payment/{bookingNumber}")]
    [HttpGet]
    public async Task<ActionResult> Index(string bookingNumber)
    {
        var booking = await LoadBookingAsync(bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }

        var hasPayPal = await IsProviderConfiguredAsync("PayPal");
        var hasStripe = await IsProviderConfiguredAsync("Stripe");

        return View(new PaymentPageViewModel
        {
            Booking = booking,
            HasPayPal = hasPayPal,
            HasStripe = hasStripe,
            IsPaymentConfigured = hasPayPal || hasStripe
        });
    }

    [Route("payment/paypal/create/{bookingNumber}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePayPal(string bookingNumber)
    {
        return await CreatePaymentAsync(bookingNumber, "PayPal");
    }

    [Route("payment/paypal/return")]
    [HttpGet]
    public async Task<ActionResult> PayPalReturn(string bookingNumber, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["PaymentNotice"] = "PayPal did not return a valid payment token. Please try again.";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }
        if (booking.Status == BookingStatus.Paid)
        {
            return RedirectToAction(nameof(Success), new { bookingNumber });
        }

        try
        {
            using var result = await CapturePayPalOrderAsync(token);
            var status = ReadString(result, "status");
            var transaction = await FindPendingTransactionAsync(booking.Id, "PayPal", token);
            if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) && transaction is not null)
            {
                await MarkPaidAsync(booking, transaction, result.RootElement.GetRawText());
                return RedirectToAction(nameof(Success), new { bookingNumber });
            }
        }
        catch (Exception ex)
        {
            TempData["PaymentNotice"] = $"PayPal payment could not be verified: {ex.Message}";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        TempData["PaymentNotice"] = "PayPal payment was not completed. Please try again.";
        return RedirectToAction(nameof(Index), new { bookingNumber });
    }

    [Route("payment/paypal/cancel")]
    [HttpGet]
    public ActionResult PayPalCancel(string bookingNumber)
    {
        TempData["PaymentNotice"] = "PayPal payment was cancelled. Your booking is still pending payment.";
        return RedirectToAction(nameof(Index), new { bookingNumber });
    }

    [Route("payment/stripe/create-checkout/{bookingNumber}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateStripeCheckout(string bookingNumber)
    {
        return await CreatePaymentAsync(bookingNumber, "Stripe");
    }

    [Route("payment/stripe/success/{bookingNumber}")]
    [HttpGet]
    public async Task<ActionResult> StripeSuccess(string bookingNumber, string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["PaymentNotice"] = "Stripe did not return a valid checkout session. Please try again.";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }
        if (booking.Status == BookingStatus.Paid)
        {
            return RedirectToAction(nameof(Success), new { bookingNumber });
        }

        try
        {
            using var result = await RetrieveStripeSessionAsync(session_id);
            var paymentStatus = ReadString(result, "payment_status");
            var transaction = await FindPendingTransactionAsync(booking.Id, "Stripe", session_id);
            if (string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase) && transaction is not null)
            {
                await MarkPaidAsync(booking, transaction, result.RootElement.GetRawText());
                return RedirectToAction(nameof(Success), new { bookingNumber });
            }
        }
        catch (Exception ex)
        {
            TempData["PaymentNotice"] = $"Stripe payment could not be verified: {ex.Message}";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        TempData["PaymentNotice"] = "Stripe payment was not completed. Please try again.";
        return RedirectToAction(nameof(Index), new { bookingNumber });
    }

    [Route("payment/stripe/cancel/{bookingNumber}")]
    [HttpGet]
    public ActionResult StripeCancel(string bookingNumber)
    {
        TempData["PaymentNotice"] = "Stripe payment was cancelled. Your booking is still pending payment.";
        return RedirectToAction(nameof(Index), new { bookingNumber });
    }

    [Route("payment/success/{bookingNumber}")]
    [HttpGet]
    public async Task<ActionResult> Success(string bookingNumber)
    {
        var booking = await LoadBookingAsync(bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }

        if (booking.Status != BookingStatus.Paid)
        {
            TempData["PaymentNotice"] = "This booking has not been paid yet.";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        return View(booking);
    }

    [Route("payment/stripe/webhook")]
    [HttpPost]
    public ActionResult StripeWebhook()
    {
        return new HttpStatusCodeResult(200);
    }

    private async Task<ActionResult> CreatePaymentAsync(string bookingNumber, string provider)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }

        if (!await IsProviderConfiguredAsync(provider))
        {
            TempData["PaymentNotice"] = $"{provider} is not configured yet. The booking has been saved for follow-up.";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }

        if (booking.Status == BookingStatus.Paid)
        {
            return RedirectToAction(nameof(Success), new { bookingNumber });
        }

        try
        {
            return provider == "Stripe"
                ? await CreateStripeCheckoutAsync(booking)
                : await CreatePayPalOrderAsync(booking);
        }
        catch (Exception ex)
        {
            TempData["PaymentNotice"] = $"{provider} payment could not be started: {ex.Message}";
            return RedirectToAction(nameof(Index), new { bookingNumber });
        }
    }

    private async Task<ActionResult> CreatePayPalOrderAsync(Booking booking)
    {
        var accessToken = await GetPayPalAccessTokenAsync();
        var amount = GetAmount(booking);
        var currency = await GetCurrencyAsync();
        var brandName = await GetBrandNameAsync();
        var returnUrl = BuildAbsoluteActionUrl(nameof(PayPalReturn), new { bookingNumber = booking.BookingNumber });
        var cancelUrl = BuildAbsoluteActionUrl(nameof(PayPalCancel), new { bookingNumber = booking.BookingNumber });
        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = booking.BookingNumber,
                    description = $"{brandName} booking {booking.BookingNumber}",
                    amount = new
                    {
                        currency_code = currency,
                        value = FormatAmount(amount)
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl,
                user_action = "PAY_NOW"
            }
        };

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{await GetPayPalBaseUrlAsync()}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent(payload);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TrimForStorage(body));
        }

        using var json = JsonDocument.Parse(body);
        var orderId = ReadString(json, "id");
        var approveUrl = ReadPayPalApproveUrl(json);
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(approveUrl))
        {
            throw new InvalidOperationException("PayPal did not return an approval URL.");
        }

        var transaction = await GetOrCreatePendingTransactionAsync(booking, "PayPal", amount);
        transaction.ProviderReference = orderId;
        transaction.RawResponse = TrimForStorage(body);
        await _db.SaveChangesAsync();
        return Redirect(approveUrl);
    }

    private async Task<ActionResult> CreateStripeCheckoutAsync(Booking booking)
    {
        var amount = GetAmount(booking);
        var currency = await GetCurrencyAsync();
        var brandName = await GetBrandNameAsync();
        var successUrl = BuildAbsoluteActionUrl(nameof(StripeSuccess), new { bookingNumber = booking.BookingNumber }) + "?session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = BuildAbsoluteActionUrl(nameof(StripeCancel), new { bookingNumber = booking.BookingNumber });
        var values = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["client_reference_id"] = booking.BookingNumber,
            ["customer_email"] = booking.Email,
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl ?? string.Empty,
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = ToMinorUnits(amount).ToString(),
            ["line_items[0][price_data][product_data][name]"] = $"{brandName} booking {booking.BookingNumber}"
        };

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _settings.GetAsync("Stripe:SecretKey"));
        request.Content = new FormUrlEncodedContent(values);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TrimForStorage(body));
        }

        using var json = JsonDocument.Parse(body);
        var sessionId = ReadString(json, "id");
        var checkoutUrl = ReadString(json, "url");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("Stripe did not return a checkout URL.");
        }

        var transaction = await GetOrCreatePendingTransactionAsync(booking, "Stripe", amount);
        transaction.ProviderReference = sessionId;
        transaction.RawResponse = TrimForStorage(body);
        await _db.SaveChangesAsync();
        return Redirect(checkoutUrl);
    }

    private async Task<string> GetPayPalAccessTokenAsync()
    {
        var clientId = await GetSettingAsync("PayPal:ClientId");
        var clientSecret = await GetSettingAsync("PayPal:ClientSecret");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{await GetPayPalBaseUrlAsync()}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TrimForStorage(body));
        }

        using var json = JsonDocument.Parse(body);
        var accessToken = ReadString(json, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("PayPal did not return an access token.");
        }

        return accessToken;
    }

    private async Task<JsonDocument> CapturePayPalOrderAsync(string orderId)
    {
        var accessToken = await GetPayPalAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{await GetPayPalBaseUrlAsync()}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent(new { });
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TrimForStorage(body));
        }

        return JsonDocument.Parse(body);
    }

    private async Task<JsonDocument> RetrieveStripeSessionAsync(string sessionId)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/checkout/sessions/{Uri.EscapeDataString(sessionId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _settings.GetAsync("Stripe:SecretKey"));
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TrimForStorage(body));
        }

        return JsonDocument.Parse(body);
    }

    private async Task MarkPaidAsync(Booking booking, PaymentTransaction transaction, string rawResponse)
    {
        booking.Status = BookingStatus.Paid;
        booking.PaidAtUtc ??= DateTime.UtcNow;
        transaction.Status = PaymentStatus.Paid;
        transaction.CompletedAtUtc = DateTime.UtcNow;
        transaction.RawResponse = TrimForStorage(rawResponse);
        await _db.SaveChangesAsync();
        await _emailService.SendBookingPaidAsync(booking);
    }

    private async Task<PaymentTransaction> GetOrCreatePendingTransactionAsync(Booking booking, string provider, decimal amount)
    {
        var transaction = await _db.PaymentTransactions
            .Where(x => x.BookingId == booking.Id && x.Provider == provider && x.Status == PaymentStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (transaction is not null)
        {
            transaction.AmountUsd = amount;
            transaction.Currency = await GetCurrencyAsync();
            return transaction;
        }

        transaction = new PaymentTransaction
        {
            BookingId = booking.Id,
            Provider = provider,
            ProviderReference = $"{provider.ToLowerInvariant()}-{Guid.NewGuid():N}",
            AmountUsd = amount,
            Currency = await GetCurrencyAsync(),
            Status = PaymentStatus.Pending
        };
        _db.PaymentTransactions.Add(transaction);
        return transaction;
    }

    private async Task<PaymentTransaction?> FindPendingTransactionAsync(int bookingId, string provider, string providerReference) =>
        await _db.PaymentTransactions
            .Where(x => x.BookingId == bookingId &&
                x.Provider == provider &&
                x.ProviderReference == providerReference &&
                x.Status == PaymentStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

    private async Task<string> GetSettingAsync(string key)
    {
        return await _settings.GetAsync(key);
    }

    private async Task<bool> IsProviderConfiguredAsync(string provider)
    {
        if (provider == "Stripe")
        {
            return !string.IsNullOrWhiteSpace(await GetSettingAsync("Stripe:PublishableKey")) &&
                !string.IsNullOrWhiteSpace(await GetSettingAsync("Stripe:SecretKey"));
        }

        return !string.IsNullOrWhiteSpace(await GetSettingAsync("PayPal:ClientId")) &&
            !string.IsNullOrWhiteSpace(await GetSettingAsync("PayPal:ClientSecret"));
    }

    private async Task<string> GetPayPalBaseUrlAsync() =>
        string.Equals(await GetSettingAsync("PayPal:Mode"), "Live", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

    private async Task<string> GetCurrencyAsync()
    {
        var currency = await _settings.GetAsync("Payment:Currency", "USD");
        return string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
    }

    private async Task<string> GetBrandNameAsync() =>
        await _settings.GetAsync("Site:BrandName", "Vietnam Transfer");

    private static decimal GetAmount(Booking booking) =>
        booking.TotalPriceUsd > 0 ? booking.TotalPriceUsd : booking.EstimatedPriceUsd;

    private static string FormatAmount(decimal amount) =>
        amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

    private static string? ReadPayPalApproveUrl(JsonDocument json)
    {
        if (!json.RootElement.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (string.Equals(ReadString(link, "rel"), "approve", StringComparison.OrdinalIgnoreCase))
            {
                return ReadString(link, "href");
            }
        }

        return null;
    }

    private static string ReadString(JsonDocument json, string propertyName) =>
        ReadString(json.RootElement, propertyName) ?? string.Empty;

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private string BuildAbsoluteActionUrl(string action, object routeValues)
    {
        var scheme = (Request.Headers["X-Forwarded-Proto"] ?? Request.Url?.Scheme ?? "https").Split(',')[0].Trim();
        var host = (Request.Headers["X-Forwarded-Host"] ?? Request.Url?.Authority ?? string.Empty).Split(',')[0].Trim();
        return Url.Action(action, "Payment", new RouteValueDictionary(routeValues), scheme, host) ?? "/";
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string TrimForStorage(string value) =>
        value.Length <= 2000 ? value : value.Substring(0, 2000);

    private async Task<Booking?> LoadBookingAsync(string bookingNumber) =>
        await _db.Bookings
            .Include(x => x.Airport)
            .Include(x => x.CarVehicleType)
            .Include(x => x.CarAddonSelections)
            .FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber);
}
