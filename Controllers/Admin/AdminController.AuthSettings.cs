using LuxuryCar.Models;
using LuxuryCar.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Host.SystemWeb;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;

namespace LuxuryCar.Controllers;

public partial class AdminController
{
    [AllowAnonymous]
    [Route("login")]
    [HttpGet]
    public ActionResult Login(string returnUrl)
    {
        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? string.Empty });
    }

    [AllowAnonymous]
    [Route("login")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl = model.ReturnUrl ?? string.Empty;

        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
        switch (result)
        {
            case SignInStatus.Success:
                return RedirectToLocal(model.ReturnUrl);
            case SignInStatus.LockedOut:
                ModelState.AddModelError(string.Empty, "This account is locked. Please contact the site administrator.");
                break;
            default:
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                break;
        }

        return View(model);
    }

    private ActionResult RedirectToLocal(string returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Dashboard));
    }

    private ActionResult RedirectToSettingsNotice(string message) =>
        RedirectToAction(nameof(Settings), new { notice = message });

    [Route("logout")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Logout()
    {
        HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
        return RedirectToAction(nameof(Login));
    }

    [Route("settings")]
    [HttpGet]
    public async Task<ActionResult> Settings(string? notice = null)
    {
        ViewData["AdminNoticeQuery"] = notice ?? string.Empty;
        return View(await BuildSettingsModelAsync());
    }

    [Route("settings")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Settings(AdminSettingsViewModel model) =>
        await SaveSiteSettings(model);

    [Route("settings/site")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveSiteSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Site:BrandName", DefaultIfBlank(model.SiteBrandName, "Vietnam Transfer"));
        await _settings.SetAsync("Site:Tagline", DefaultIfBlank(model.SiteTagline, "Private Transfer"));
        await _settings.SetAsync("Site:Hotline", DefaultIfBlank(model.SiteHotline, "1900 8888"));
        await _settings.SetAsync("Site:ContactEmail", DefaultIfBlank(model.SiteContactEmail, "info@vietnamtransfer.vn"));
        await _settings.SetAsync("Site:Address", DefaultIfBlank(model.SiteAddress, "123 Nguyen Hue, District 1, Ho Chi Minh City"));
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Public site settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/theme")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveThemeSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Theme:Ink", NormalizeHexColor(model.ThemeInkColor, "#151515"));
        await _settings.SetAsync("Theme:Muted", NormalizeHexColor(model.ThemeMutedColor, "#6f6f6f"));
        await _settings.SetAsync("Theme:Accent", NormalizeHexColor(model.ThemeAccentColor, "#c89b3c"));
        await _settings.SetAsync("Theme:AccentDark", NormalizeHexColor(model.ThemeAccentDarkColor, "#a87920"));
        await _settings.SetAsync("Theme:Paper", NormalizeHexColor(model.ThemePaperColor, "#ffffff"));
        await _settings.SetAsync("Theme:Soft", NormalizeHexColor(model.ThemeSoftColor, "#f7f5f0"));
        await _settings.SetAsync("Theme:Line", NormalizeHexColor(model.ThemeLineColor, "#e9e4d9"));
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Website color theme saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/email")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveEmailSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Email:SmtpHost", model.EmailSmtpHost);
        await _settings.SetAsync("Email:SmtpPort", Clamp(model.EmailSmtpPort, 1, 65535).ToString());
        await _settings.SetAsync("Email:EnableSsl", model.EmailEnableSsl.ToString());
        await _settings.SetAsync("Email:Username", model.EmailUsername);
        await _settings.SetAsync("Email:From", DefaultIfBlank(model.EmailFrom, "bookings@vietnamtransfer.local"));
        await SaveSecretAsync("Email:Password", model.EmailPassword, model.ClearEmailPassword);
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "SMTP settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/email/test")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SendTestEmail(AdminSettingsViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TestEmailTo))
        {
            TempData["AdminNotice"] = "Enter a recipient email address before sending a test.";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            await _emailService.SendTestEmailAsync(model.TestEmailTo.Trim());
            TempData["AdminNotice"] = $"Test email sent to {model.TestEmailTo.Trim()}.";
        }
        catch (Exception ex)
        {
            TempData["AdminNotice"] = $"Test email failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/cloudinary")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveCloudinarySettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Cloudinary:CloudName", model.CloudinaryCloudName);
        await _settings.SetAsync("Cloudinary:ApiKey", model.CloudinaryApiKey);
        await _settings.SetAsync("Cloudinary:Folder", DefaultIfBlank(model.CloudinaryFolder, "vietnamtransfer"));
        await SaveSecretAsync("Cloudinary:ApiSecret", model.CloudinaryApiSecret, model.ClearCloudinaryApiSecret);
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Cloudinary settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/geoapify")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveGeoapifySettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Geoapify:ApiKey", model.GeoapifyApiKey);
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Geoapify settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/paypal")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SavePayPalSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("PayPal:ClientId", model.PayPalClientId);
        await _settings.SetAsync("PayPal:Mode", string.Equals(model.PayPalMode, "Live", StringComparison.OrdinalIgnoreCase) ? "Live" : "Sandbox");
        await SaveSecretAsync("PayPal:ClientSecret", model.PayPalClientSecret, model.ClearPayPalClientSecret);
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "PayPal settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/stripe")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveStripeSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Stripe:PublishableKey", model.StripePublishableKey);
        await SaveSecretAsync("Stripe:SecretKey", model.StripeSecretKey, model.ClearStripeSecretKey);
        await SaveSecretAsync("Stripe:WebhookSecret", model.StripeWebhookSecret, model.ClearStripeWebhookSecret);
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Stripe settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/booking")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveBookingSettings(AdminSettingsViewModel model)
    {
        await _settings.SetAsync("Booking:TaxFeeRate", Clamp(model.BookingTaxFeeRate, 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settings.SetAsync("Booking:CouponCode", model.BookingCouponCode);
        await _settings.SetAsync("Booking:DiscountRate", Clamp(model.BookingDiscountRate, 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settings.SetAsync("Payment:Currency", DefaultIfBlank(model.PaymentCurrency, "USD").ToUpperInvariant());
        await _settings.SaveChangesAsync();
        TempData["AdminNotice"] = "Booking and payment defaults saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/password")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeAdminPassword(AdminSettingsViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
            string.IsNullOrWhiteSpace(model.NewPassword) ||
            model.NewPassword.Length < 8 ||
            model.NewPassword != model.ConfirmPassword)
        {
            TempData["AdminNotice"] = "Password change failed. Check the current password and make sure the new password is at least 8 characters and confirmed.";
            return RedirectToAction(nameof(Settings));
        }

        var userId = User.Identity.GetUserId();
        var user = string.IsNullOrWhiteSpace(userId) ? null : await UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await UserManager.ChangePasswordAsync(user.Id, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            TempData["AdminNotice"] = "Password change failed: " + string.Join(" ", result.Errors);
            return RedirectToAction(nameof(Settings));
        }

        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        TempData["AdminNotice"] = "Admin password changed.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/admin-access")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveAdminAccess(AdminSettingsViewModel model)
    {
        var wantsEmailChange = !string.IsNullOrWhiteSpace(model.NewAdminEmail) ||
            !string.IsNullOrWhiteSpace(model.ConfirmAdminEmail);
        var wantsPasswordChange = !string.IsNullOrWhiteSpace(model.NewPassword) ||
            !string.IsNullOrWhiteSpace(model.ConfirmPassword);

        if (!wantsEmailChange && !wantsPasswordChange)
        {
            return RedirectToSettingsNotice("No admin access changes submitted.");
        }

        if (string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            return RedirectToSettingsNotice("Enter the current password to change admin access.");
        }

        var userId = User.Identity.GetUserId();
        var user = string.IsNullOrWhiteSpace(userId) ? null : await UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await UserManager.CheckPasswordAsync(user, model.CurrentPassword))
        {
            return RedirectToSettingsNotice("Admin access change failed. Current password is incorrect.");
        }

        var notices = new List<string>();
        if (wantsEmailChange)
        {
            var newEmail = model.NewAdminEmail?.Trim();
            var confirmEmail = model.ConfirmAdminEmail?.Trim();
            if (string.IsNullOrWhiteSpace(newEmail) ||
                !string.Equals(newEmail, confirmEmail, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToSettingsNotice("Email change failed. Enter the new email twice and make sure both values match.");
            }

            var existing = await UserManager.FindByEmailAsync(newEmail);
            if (existing is not null && !string.Equals(existing.Id, user.Id, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToSettingsNotice("Email change failed. This email is already used by another account.");
            }

            user.Email = newEmail;
            user.UserName = newEmail;
            user.EmailConfirmed = true;
            var emailResult = await UserManager.UpdateAsync(user);
            if (!emailResult.Succeeded)
            {
                return RedirectToSettingsNotice("Email change failed: " + string.Join(" ", emailResult.Errors));
            }

            notices.Add($"Admin login email changed to {newEmail}.");
        }

        if (wantsPasswordChange)
        {
            if (string.IsNullOrWhiteSpace(model.NewPassword) ||
                model.NewPassword.Length < 8 ||
                model.NewPassword != model.ConfirmPassword)
            {
                return RedirectToSettingsNotice("Password change failed. Make sure the new password is at least 8 characters and confirmed.");
            }

            var passwordResult = await UserManager.ChangePasswordAsync(user.Id, model.CurrentPassword, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return RedirectToSettingsNotice("Password change failed: " + string.Join(" ", passwordResult.Errors));
            }

            user = await UserManager.FindByIdAsync(user.Id);
            notices.Add("Admin password changed.");
        }

        if (user is not null)
        {
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }

        return RedirectToSettingsNotice(string.Join(" ", notices));
    }

    [Route("settings/login-email")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeAdminLoginEmail(AdminSettingsViewModel model)
    {
        var newEmail = model.NewAdminEmail?.Trim();
        var confirmEmail = model.ConfirmAdminEmail?.Trim();
        if (string.IsNullOrWhiteSpace(newEmail) ||
            !string.Equals(newEmail, confirmEmail, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(model.AdminEmailCurrentPassword))
        {
            TempData["AdminNotice"] = "Email change failed. Enter the new email twice and confirm with the current password.";
            return RedirectToAction(nameof(Settings));
        }

        var userId = User.Identity.GetUserId();
        var user = string.IsNullOrWhiteSpace(userId) ? null : await UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await UserManager.CheckPasswordAsync(user, model.AdminEmailCurrentPassword))
        {
            TempData["AdminNotice"] = "Email change failed. Current password is incorrect.";
            return RedirectToAction(nameof(Settings));
        }

        var existing = await UserManager.FindByEmailAsync(newEmail);
        if (existing is not null && !string.Equals(existing.Id, user.Id, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminNotice"] = "Email change failed. This email is already used by another account.";
            return RedirectToAction(nameof(Settings));
        }

        user.Email = newEmail;
        user.UserName = newEmail;
        user.EmailConfirmed = true;
        var result = await UserManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["AdminNotice"] = "Email change failed: " + string.Join(" ", result.Errors);
            return RedirectToAction(nameof(Settings));
        }

        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        TempData["AdminNotice"] = $"Admin login email changed to {newEmail}.";
        return RedirectToAction(nameof(Settings));
    }

    [Route("settings/cloudinary/sync")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SyncCloudinaryMedia()
    {
        if (!await _mediaStorage.IsConfiguredAsync())
        {
            TempData["AdminNotice"] = "Cloudinary is not configured yet.";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            var result = await _mediaStorage.SyncImagesAsync();
            TempData["AdminNotice"] = $"Cloudinary sync completed. Imported {result.ImportedCount}, skipped {result.SkippedCount}.";
        }
        catch (Exception ex)
        {
            TempData["AdminNotice"] = $"Cloudinary sync failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [Route("")]
    [HttpGet]
    [Route("dashboard")]
    public async Task<ActionResult> Dashboard()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var bookings = _db.Bookings.AsNoTracking();
        var groupedStatuses = await bookings
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalBookings = await bookings.CountAsync(),
            PendingPaymentBookings = await bookings.CountAsync(x => x.Status == BookingStatus.PendingPayment),
            PaidBookings = await bookings.CountAsync(x => x.Status == BookingStatus.Paid),
            TodayBookings = await bookings.CountAsync(x => x.CreatedAtUtc >= todayUtc),
            EstimatedRevenueUsd = await bookings
                .Where(x => x.Status == BookingStatus.Paid)
                .Select(x => (decimal?)x.EstimatedPriceUsd)
                .SumAsync() ?? 0m,
            RecentBookings = await _db.Bookings
                .Include(x => x.Airport)
                .Include(x => x.CarVehicleType)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(10)
                .AsNoTracking()
                .ToListAsync(),
            StatusCounts = groupedStatuses.ToDictionary(x => x.Status, x => x.Count)
        };

        return View(model);
    }

}
