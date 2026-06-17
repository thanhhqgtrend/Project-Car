using LuxuryCar.Data;
using LuxuryCar.Models;
using LuxuryCar.Services;
using LuxuryCar.ViewModels;
using System.Web.Mvc;
using System.Data.Entity;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace LuxuryCar.Controllers;

public class BookingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IQuoteService _quoteService;
    private readonly IBookingNumberService _bookingNumberService;
    private readonly IEmailService _emailService;
    private readonly IAppSettingService _settings;

    public BookingController(
        ApplicationDbContext db,
        IQuoteService quoteService,
        IBookingNumberService bookingNumberService,
        IEmailService emailService,
        IAppSettingService settings)
    {
        _db = db;
        _quoteService = quoteService;
        _bookingNumberService = bookingNumberService;
        _emailService = emailService;
        _settings = settings;
    }

    [HttpGet]
    [Route("booking/search")]
    public async Task<ActionResult> Search(BookingSearchViewModel model)
    {
        await PopulateSearchOptionsAsync(model);
        if (model.SearchSubmitted && ValidateSearch(model))
        {
            await PopulateCarVehicleOptionsAsync(model);
        }

        return View(model);
    }

    [Route("booking/results")]
    [HttpGet]
    public async Task<ActionResult> Results(BookingSearchViewModel model)
    {
        await PopulateSearchOptionsAsync(model);
        if (!ValidateSearch(model))
        {
            return View("Search", model);
        }

        var results = new BookingResultsViewModel
        {
            SearchSubmitted = true,
            TripType = model.TripType,
            AirportId = model.AirportId,
            OriginAddress = model.OriginAddress,
            DestinationAddress = model.DestinationAddress,
            PickupDateTime = model.PickupDateTime,
            PassengerCount = model.PassengerCount,
            Airports = model.Airports,
            GeoapifyApiKey = model.GeoapifyApiKey
        };

        await PopulateCarVehicleOptionsAsync(results);
        return View(results);
    }

    private async Task PopulateCarVehicleOptionsAsync(BookingSearchViewModel model)
    {
        model.CarVehicleOptions.Clear();
        Airport? airport = null;
        if (model.AirportId.HasValue && model.TripType is TripType.AirportPickup or TripType.AirportDropoff)
        {
            airport = await _db.Airports.FindAsync(model.AirportId.Value);
        }

        var distance = await GetDistanceForSearchAsync(model, airport);
        var taxFeeRate = await _settings.GetDecimalAsync("Booking:TaxFeeRate", 0.08m);
        var vehicles = await _db.CarVehicleTypes
            .Include(x => x.Image)
            .Where(x => x.IsActive && x.PassengerCapacity >= model.PassengerCount)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            decimal basePrice;
            decimal distanceKm = 0;
            DistanceStatus distanceStatus = DistanceStatus.Estimated;

            if (model.TripType == TripType.Hire)
            {
                basePrice = vehicle.DailyRateUsd * model.HireTotalDays;
            }
            else
            {
                var quote = distance is null
                    ? _quoteService.QuoteHire(vehicle)
                    : _quoteService.QuoteFromDistance(vehicle, distance);
                basePrice = quote.PriceUsd;
                distanceKm = quote.DistanceKm;
                distanceStatus = quote.DistanceStatus;
            }

            var taxFee = Math.Round(Math.Max(0, basePrice) * taxFeeRate, 2);
            model.CarVehicleOptions.Add(new CarVehicleQuoteOption
            {
                CarVehicle = vehicle,
                DistanceKm = distanceKm,
                DistanceStatus = distanceStatus,
                BasePriceUsd = basePrice,
                TaxFeeUsd = taxFee,
                TotalPriceUsd = basePrice + taxFee
            });
        }
    }

    private async Task<QuoteDistanceResult?> GetDistanceForSearchAsync(BookingSearchViewModel model, Airport? airport)
    {
        if (model.TripType == TripType.Hire)
        {
            return null;
        }

        if (model.TripType == TripType.PointToPoint)
        {
            return await _quoteService.GetRouteDistanceAsync(model.OriginAddress ?? string.Empty, model.DestinationAddress ?? string.Empty);
        }

        if (airport is null)
        {
            return new QuoteDistanceResult(0, DistanceStatus.Failed);
        }

        var address = model.TripType == TripType.AirportDropoff ? model.OriginAddress : model.DestinationAddress;
        return await _quoteService.GetAirportDistanceAsync(airport, address ?? string.Empty);
    }

    [Route("booking/checkout")]
    [HttpGet]
    public async Task<ActionResult> Checkout(BookingCheckoutViewModel model)
    {
        await PopulateCheckoutAsync(model);
        if (!ValidateSearch(model))
        {
            return View("Search", model);
        }
        ClearPassengerValidationState();

        if (model.CarVehicle is null)
        {
            return HttpNotFound();
        }

        await ApplyPriceAsync(model);
        return View(model);
    }

    private void ClearPassengerValidationState()
    {
        foreach (var key in new[]
        {
            nameof(BookingCheckoutViewModel.FirstName),
            nameof(BookingCheckoutViewModel.LastName),
            nameof(BookingCheckoutViewModel.FullName),
            nameof(BookingCheckoutViewModel.Email),
            nameof(BookingCheckoutViewModel.Phone),
            nameof(BookingCheckoutViewModel.Title),
            nameof(BookingCheckoutViewModel.PickupTime),
            nameof(BookingCheckoutViewModel.TermsAccepted)
        })
        {
            ModelState.Remove(key);
        }
    }

    [Route("booking/checkout")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CheckoutPost(BookingCheckoutViewModel model)
    {
        await PopulateCheckoutAsync(model);
        NormalizePassengerName(model);
        ApplyPickupTime(model);
        ValidateSearch(model);

        if (model.CarVehicle is null)
        {
            ModelState.AddModelError(nameof(model.CarVehicleTypeId), "Please select a valid vehicle.");
        }
        else if (model.PassengerCount > model.CarVehicle.PassengerCapacity)
        {
            ModelState.AddModelError(nameof(model.PassengerCount), $"This vehicle accepts up to {model.CarVehicle.PassengerCapacity} passengers.");
        }

        if (!model.TermsAccepted)
        {
            ModelState.AddModelError(nameof(model.TermsAccepted), "Please accept the terms and conditions.");
        }

        await ApplyPriceAsync(model);
        if (!ModelState.IsValid || model.CarVehicle is null)
        {
            return View("Checkout", model);
        }

        var airport = model.AirportId.HasValue ? await _db.Airports.FindAsync(model.AirportId.Value) : null;
        var booking = new Booking
        {
            BookingNumber = await _bookingNumberService.NextAsync(),
            TripType = model.TripType,
            AirportId = model.AirportId,
            CarVehicleTypeId = model.CarVehicleTypeId,
            PickupAddress = ResolvePickupAddress(model, airport),
            DropoffAddress = ResolveDropoffAddress(model, airport),
            PickupDateTimeUtc = ToUtcFromVietnamTime(model.PickupDateTime),
            PassengerCount = model.PassengerCount,
            DistanceKm = model.DistanceKm,
            DistanceStatus = model.DistanceStatus,
            BasePriceUsd = model.BasePriceUsd,
            AddonTotalUsd = model.AddonTotalUsd,
            DiscountUsd = model.DiscountUsd,
            TaxFeeUsd = model.TaxFeeUsd,
            TotalPriceUsd = model.TotalPriceUsd,
            EstimatedPriceUsd = model.TotalPriceUsd,
            Currency = await _settings.GetAsync("Payment:Currency", "USD"),
            CouponCode = model.CouponCode?.Trim() ?? string.Empty,
            TermsAccepted = model.TermsAccepted,
            Title = model.Title,
            FullName = model.FullName ?? string.Empty,
            Email = model.Email,
            Phone = $"{model.CountryCode} {model.Phone}".Trim(),
            FlightNumber = model.FlightNumber?.Trim() ?? string.Empty,
            MessagingApp = model.MessagingApp,
            MessagingHandle = model.MessagingHandle ?? string.Empty,
            Notes = model.Notes ?? string.Empty,
            Status = BookingStatus.PendingPayment,
            HireStartDate = model.TripType == TripType.Hire ? model.StartDate : (DateTime?)null,
            HireEndDate = model.TripType == TripType.Hire ? model.EndDate : (DateTime?)null,
            HireTotalDays = model.TripType == TripType.Hire ? model.HireTotalDays : 1,
        };

        foreach (var addon in GetSelectedAddonCharges(model))
        {
            booking.CarAddonSelections.Add(new CarBookingAddonSelection
            {
                CarBookingAddonId = addon.Addon.Id,
                Name = addon.Addon.Name,
                PricingMode = addon.Addon.PricingMode,
                Quantity = addon.Quantity,
                IncludedQuantity = addon.Addon.IncludedQuantity,
                UnitPriceUsd = addon.Addon.PriceUsd,
                PriceUsd = addon.TotalUsd
            });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        GrantBookingAccess(booking);
        await _emailService.SendBookingCreatedAsync(booking);

        return RedirectToAction("Index", "Payment", new { bookingNumber = booking.BookingNumber });
    }

    [Route("booking/confirmation/{bookingNumber}")]
    [HttpGet]
    public async Task<ActionResult> Confirmation(string bookingNumber)
    {
        var booking = await LoadBookingAsync(bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }

        var isVerified = HasBookingAccess(booking);
        return View(new BookingConfirmationViewModel
        {
            Booking = booking,
            IsPaymentConfigured = await IsPaymentConfiguredAsync(),
            IsVerified = isVerified
        });
    }

    [Route("booking/track")]
    [HttpGet]
    public ActionResult Track()
    {
        return RedirectToAction("Search");
    }


    [Route("booking/track/check")]
    [HttpGet]
    public async Task<ActionResult> TrackCheck(string bookingNumber, string emailOrPhone)
    {
        if (string.IsNullOrWhiteSpace(bookingNumber) || string.IsNullOrWhiteSpace(emailOrPhone))
            return Json(new { found = false, error = "Please enter your booking number and email or phone." }, JsonRequestBehavior.AllowGet);

        var booking = await _db.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber.Trim());

        if (booking == null || !MatchesBookingContact(booking, emailOrPhone))
            return Json(new { found = false, error = "Booking not found. Please check your details." }, JsonRequestBehavior.AllowGet);

        return Json(new { found = true }, JsonRequestBehavior.AllowGet);
    }

    [Route("booking/track/result")]
    [HttpGet]
    public async Task<ActionResult> TrackResult(string bookingNumber, string emailOrPhone)
    {
        var booking = await _db.Bookings
            .Include(x => x.Airport)
            .Include(x => x.CarVehicleType)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber.Trim());

        if (booking == null || !MatchesBookingContact(booking, emailOrPhone))
            return RedirectToAction("Search");

        return View("TrackResult", booking);
    }
    [Route("booking/confirmation/{bookingNumber}/verify")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> VerifyConfirmation(string bookingNumber, BookingConfirmationViewModel model)
    {
        var booking = await LoadBookingAsync(bookingNumber);
        if (booking is null)
        {
            return HttpNotFound();
        }

        if (MatchesBookingContact(booking, model.EmailOrPhone))
        {
            GrantBookingAccess(booking);
            return RedirectToAction(nameof(Confirmation), new { bookingNumber });
        }

        ModelState.AddModelError(nameof(model.EmailOrPhone), "Booking details could not be verified.");
        return View("Confirmation", new BookingConfirmationViewModel
        {
            Booking = new Booking { BookingNumber = booking.BookingNumber },
            IsPaymentConfigured = await IsPaymentConfiguredAsync(),
            IsVerified = false,
            EmailOrPhone = model.EmailOrPhone?.Trim() ?? string.Empty
        });
    }

    private async Task PopulateSearchOptionsAsync(BookingSearchViewModel model)
    {
        model.Airports = await _db.Airports.Where(x => x.IsActive)
            .OrderBy(x => x.City)
            .Select(x => new SelectListItem { Text = x.City + " - " + x.Name + " (" + x.Code + ")", Value = x.Id.ToString() })
            .ToListAsync();
        model.GeoapifyApiKey = await GetSettingAsync("Geoapify:ApiKey");
    }

    private async Task PopulateCheckoutAsync(BookingCheckoutViewModel model)
    {
        await PopulateSearchOptionsAsync(model);
        model.CarVehicle = await _db.CarVehicleTypes.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == model.CarVehicleTypeId && x.IsActive);
        model.Addons = await _db.CarBookingAddons.Include(x => x.Image).Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();
        if (string.IsNullOrWhiteSpace(model.PickupTime) && model.PickupDateTime.TimeOfDay != TimeSpan.Zero)
        {
            model.PickupTime = model.PickupDateTime.ToString("HH:mm");
        }
    }

    private static void NormalizePassengerName(BookingCheckoutViewModel model)
    {
        model.FirstName = model.FirstName?.Trim() ?? string.Empty;
        model.LastName = model.LastName?.Trim() ?? string.Empty;
        model.FullName = string.Join(" ", new[] { model.FirstName, model.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private void ApplyPickupTime(BookingCheckoutViewModel model)
    {
        model.PickupTime = model.PickupTime?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model.PickupTime))
        {
            ModelState.AddModelError(nameof(model.PickupTime), "Please enter the pickup time.");
            return;
        }

        if (!TimeSpan.TryParse(model.PickupTime, out var pickupTime) || pickupTime < TimeSpan.Zero || pickupTime >= TimeSpan.FromDays(1))
        {
            ModelState.AddModelError(nameof(model.PickupTime), "Please enter a valid pickup time.");
            return;
        }

        model.PickupDateTime = model.PickupDateTime.Date.Add(pickupTime);
        ModelState.SetModelValue(nameof(model.PickupDateTime), new ValueProviderResult(model.PickupDateTime, model.PickupDateTime.ToString("yyyy-MM-ddTHH:mm"), CultureInfo.InvariantCulture));
    }

    private bool ValidateSearch(BookingSearchViewModel model)
    {
        NormalizeSearchForTrip(model);
        var isValid = true;

        if (model.PassengerCount < 1)
        {
            ModelState.AddModelError(nameof(model.PassengerCount), "Please enter at least one passenger.");
            isValid = false;
        }

        if (model.PickupDateTime.Date < VietnamToday())
        {
            ModelState.AddModelError(nameof(model.PickupDateTime), "Please select today or a future pickup date.");
            isValid = false;
        }

        if ((model.TripType == TripType.AirportPickup || model.TripType == TripType.AirportDropoff) && !model.AirportId.HasValue)
        {
            ModelState.AddModelError(nameof(model.AirportId), "Please select an airport.");
            isValid = false;
        }

        if (model.TripType == TripType.AirportPickup && string.IsNullOrWhiteSpace(model.DestinationAddress))
        {
            ModelState.AddModelError(nameof(model.DestinationAddress), "Please enter the dropoff point.");
            isValid = false;
        }

        if (model.TripType == TripType.AirportDropoff && string.IsNullOrWhiteSpace(model.OriginAddress))
        {
            ModelState.AddModelError(nameof(model.OriginAddress), "Please enter the pickup point.");
            isValid = false;
        }

        if (model.TripType == TripType.PointToPoint)
        {
            if (string.IsNullOrWhiteSpace(model.OriginAddress))
            {
                ModelState.AddModelError(nameof(model.OriginAddress), "Please enter point A.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(model.DestinationAddress))
            {
                ModelState.AddModelError(nameof(model.DestinationAddress), "Please enter point B.");
                isValid = false;
            }
        }

        if (model.TripType == TripType.Hire && string.IsNullOrWhiteSpace(model.OriginAddress))
        {
            ModelState.AddModelError(nameof(model.OriginAddress), "Please enter the pickup point.");
            isValid = false;
        }

        return isValid;
    }

    private void NormalizeSearchForTrip(BookingSearchViewModel model)
    {
        model.OriginAddress = model.OriginAddress?.Trim() ?? string.Empty;
        model.DestinationAddress = model.DestinationAddress?.Trim() ?? string.Empty;

        switch (model.TripType)
        {
            case TripType.AirportPickup:
                model.OriginAddress = string.Empty;
                RemoveSearchFieldErrors(nameof(model.OriginAddress));
                break;
            case TripType.AirportDropoff:
                model.DestinationAddress = string.Empty;
                RemoveSearchFieldErrors(nameof(model.DestinationAddress));
                break;
            case TripType.PointToPoint:
                model.AirportId = null;
                RemoveSearchFieldErrors(nameof(model.AirportId));
                break;
            case TripType.Hire:
                model.AirportId = null;
                model.DestinationAddress = string.Empty;
                RemoveSearchFieldErrors(nameof(model.AirportId));
                RemoveSearchFieldErrors(nameof(model.DestinationAddress));
                break;
        }
    }

    private void RemoveSearchFieldErrors(string fieldName)
    {
        ModelState.Remove(fieldName);
        ModelState.Remove($"model.{fieldName}");
    }

    private async Task ApplyPriceAsync(BookingCheckoutViewModel model)
    {
        if (model.CarVehicle is null)
        {
            return;
        }

        var quote = await QuoteForSearchAsync(model, model.CarVehicle);
        var selectedAddons = GetSelectedAddonCharges(model);
        model.DistanceKm = quote.DistanceKm;
        model.DistanceStatus = quote.DistanceStatus;
        model.BasePriceUsd = quote.PriceUsd;
        model.AddonTotalUsd = selectedAddons.Sum(x => x.TotalUsd);
        model.DiscountUsd = await CalculateDiscountAsync(model.CouponCode ?? string.Empty, model.BasePriceUsd + model.AddonTotalUsd);
        model.TaxFeeUsd = await CalculateTaxFeeAsync(model.BasePriceUsd + model.AddonTotalUsd - model.DiscountUsd);
        model.TotalPriceUsd = Math.Max(0, model.BasePriceUsd + model.AddonTotalUsd - model.DiscountUsd + model.TaxFeeUsd);
    }

    private List<AddonCharge> GetSelectedAddonCharges(BookingCheckoutViewModel model)
    {
        return model.Addons
            .Where(x => model.SelectedAddonIds.Contains(x.Id))
            .Select(addon =>
            {
                var quantity = GetAddonQuantity(model, addon);
                var chargeableQuantity = addon.PricingMode == AddonPricingMode.Quantity
                    ? Math.Max(0, quantity - addon.IncludedQuantity)
                    : quantity;
                return new AddonCharge(addon, quantity, Math.Round(chargeableQuantity * addon.PriceUsd, 2));
            })
            .ToList();
    }

    private static int GetAddonQuantity(BookingCheckoutViewModel model, CarBookingAddon addon)
    {
        if (addon.PricingMode == AddonPricingMode.PerPassenger)
        {
            return Math.Max(1, model.PassengerCount);
        }

        if (addon.PricingMode == AddonPricingMode.Quantity)
        {
            int quantity;
            return Math.Max(1, model.AddonQuantities.TryGetValue(addon.Id, out quantity) ? quantity : 1);
        }

        return 1;
    }

    private sealed record AddonCharge(CarBookingAddon Addon, int Quantity, decimal TotalUsd);

    private async Task<QuoteResult> QuoteForSearchAsync(BookingSearchViewModel model, CarVehicleType vehicle)
    {
        if (model.TripType == TripType.Hire)
        {
            var totalPrice = vehicle.DailyRateUsd * model.HireTotalDays;
            return new QuoteResult(0, DistanceStatus.Estimated, totalPrice);
        }

        if (model.TripType == TripType.PointToPoint)
        {
            return await _quoteService.QuoteRouteAsync(model.OriginAddress ?? string.Empty, model.DestinationAddress ?? string.Empty, vehicle);
        }

        var airport = model.AirportId.HasValue ? await _db.Airports.FindAsync(model.AirportId.Value) : null;
        if (airport is null)
        {
            return new QuoteResult(0, DistanceStatus.Failed, vehicle.BaseFareUsd);
        }

        var address = model.TripType == TripType.AirportDropoff ? model.OriginAddress : model.DestinationAddress;
        return await _quoteService.QuoteAsync(airport, vehicle, address ?? string.Empty);
    }

    private static string ResolvePickupAddress(BookingSearchViewModel model, Airport? airport) =>
        model.TripType switch
        {
            TripType.AirportPickup => airport?.Name ?? string.Empty,
            TripType.AirportDropoff => model.OriginAddress ?? string.Empty,
            _ => model.OriginAddress ?? string.Empty
        };

    private static string ResolveDropoffAddress(BookingSearchViewModel model, Airport? airport) =>
        model.TripType switch
        {
            TripType.AirportPickup => model.DestinationAddress ?? string.Empty,
            TripType.AirportDropoff => airport?.Name ?? string.Empty,
            TripType.Hire => "Car hire",
            _ => model.DestinationAddress ?? string.Empty
        };

    private async Task<decimal> CalculateTaxFeeAsync(decimal subtotal)
    {
        var taxFeeRate = await _settings.GetDecimalAsync("Booking:TaxFeeRate", 0.08m);
        return Math.Round(Math.Max(0, subtotal) * Clamp(taxFeeRate, 0, 1), 2);
    }

    private async Task<decimal> CalculateDiscountAsync(string couponCode, decimal subtotal)
    {
        var configuredCode = await _settings.GetAsync("Booking:CouponCode", "VIETNAM10");
        if (string.IsNullOrWhiteSpace(configuredCode) ||
            !string.Equals(couponCode?.Trim(), configuredCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var discountRate = await _settings.GetDecimalAsync("Booking:DiscountRate", 0.10m);
        return Math.Round(subtotal * Clamp(discountRate, 0, 1), 2);
    }

    private async Task<bool> IsPaymentConfiguredAsync() =>
        (!string.IsNullOrWhiteSpace(await _settings.GetAsync("PayPal:ClientId")) &&
            !string.IsNullOrWhiteSpace(await _settings.GetAsync("PayPal:ClientSecret"))) ||
        (!string.IsNullOrWhiteSpace(await _settings.GetAsync("Stripe:PublishableKey")) &&
            !string.IsNullOrWhiteSpace(await _settings.GetAsync("Stripe:SecretKey")));

    private async Task<string> GetSettingAsync(string key)
    {
        return await _settings.GetAsync(key);
    }

    private async Task<Booking?> LoadBookingAsync(string bookingNumber) =>
        await _db.Bookings
            .Include(x => x.Airport)
            .Include(x => x.CarVehicleType)
            .Include(x => x.CarAddonSelections)
            .FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber);

    private void GrantBookingAccess(Booking booking)
    {
        var protectedBytes = MachineKey.Protect(Encoding.UTF8.GetBytes($"{booking.Id}|{booking.BookingNumber}"), "booking-confirmation-access-v1");
        var protectedValue = Convert.ToBase64String(protectedBytes);
        var cookie = new HttpCookie(BookingAccessCookieName(booking.BookingNumber), protectedValue)
        {
            HttpOnly = true,
            Secure = Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(30)
        };
        Response.Cookies.Add(cookie);
    }

    private bool HasBookingAccess(Booking booking)
    {
        var cookie = Request.Cookies[BookingAccessCookieName(booking.BookingNumber)];
        var protectedValue = cookie?.Value;
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return false;
        }

        try
        {
            var unprotected = MachineKey.Unprotect(Convert.FromBase64String(protectedValue), "booking-confirmation-access-v1");
            return Encoding.UTF8.GetString(unprotected) == $"{booking.Id}|{booking.BookingNumber}";
        }
        catch
        {
            return false;
        }
    }

    private static string BookingAccessCookieName(string bookingNumber) =>
        $"booking_access_{SanitizeCookieName(bookingNumber)}";

    private static string SanitizeCookieName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static bool MatchesBookingContact(Booking booking, string? input)
    {
        var candidate = input?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (string.Equals(candidate, booking.Email, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var candidatePhone = NormalizePhone(candidate);
        var bookingPhone = NormalizePhone(booking.Phone);
        if (string.IsNullOrWhiteSpace(candidatePhone) || string.IsNullOrWhiteSpace(bookingPhone))
        {
            return false;
        }

        return candidatePhone == bookingPhone ||
            TrimVietnamCountryCode(candidatePhone) == TrimVietnamCountryCode(bookingPhone);
    }

    private static string NormalizePhone(string value) =>
        Regex.Replace(value, "[^0-9]", string.Empty);

    private static string TrimVietnamCountryCode(string value)
    {
        if (value.StartsWith("84", StringComparison.Ordinal) && value.Length > 2)
        {
            return $"0{value.Substring(2)}";
        }

        return value;
    }

    private static DateTime ToUtcFromVietnamTime(DateTime localDateTime)
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }

        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), zone);
    }

    private static DateTime VietnamToday() => DateTime.UtcNow.AddHours(7).Date;

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;
}