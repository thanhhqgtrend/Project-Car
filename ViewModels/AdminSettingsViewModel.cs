using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.ViewModels;

public class AdminSettingsViewModel
{
    [StringLength(120)]
    public string? SiteBrandName { get; set; } = "Vietnam Transfer";

    [StringLength(160)]
    public string? SiteTagline { get; set; } = "Private Transfer";

    [StringLength(60)]
    public string? SiteHotline { get; set; } = "1900 8888";

    [EmailAddress, StringLength(160)]
    public string? SiteContactEmail { get; set; } = "info@vietnamtransfer.vn";

    [StringLength(240)]
    public string? SiteAddress { get; set; } = "123 Nguyen Hue, District 1, Ho Chi Minh City";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeInkColor { get; set; } = "#151515";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeMutedColor { get; set; } = "#6f6f6f";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeAccentColor { get; set; } = "#c89b3c";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeAccentDarkColor { get; set; } = "#a87920";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemePaperColor { get; set; } = "#ffffff";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeSoftColor { get; set; } = "#f7f5f0";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string ThemeLineColor { get; set; } = "#e9e4d9";

    [StringLength(160)]
    public string? EmailSmtpHost { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int EmailSmtpPort { get; set; } = 587;

    public bool EmailEnableSsl { get; set; } = true;

    [StringLength(160)]
    public string? EmailUsername { get; set; } = string.Empty;

    [StringLength(240)]
    public string? EmailPassword { get; set; } = string.Empty;

    public bool ClearEmailPassword { get; set; }

    [EmailAddress, StringLength(160)]
    public string? EmailFrom { get; set; } = "bookings@vietnamtransfer.local";

    [EmailAddress, StringLength(160)]
    public string? TestEmailTo { get; set; } = string.Empty;

    public bool HasEmailPassword { get; set; }

    public bool IsEmailConfigured { get; set; }

    [StringLength(160)]
    public string? CloudinaryCloudName { get; set; } = string.Empty;

    [StringLength(160)]
    public string? CloudinaryApiKey { get; set; } = string.Empty;

    [StringLength(240)]
    public string? CloudinaryApiSecret { get; set; } = string.Empty;

    public bool ClearCloudinaryApiSecret { get; set; }

    [StringLength(160)]
    public string? CloudinaryFolder { get; set; } = "vietnamtransfer";

    public bool HasCloudinaryApiSecret { get; set; }

    public bool IsCloudinaryConfigured { get; set; }

    [StringLength(240)]
    public string? GeoapifyApiKey { get; set; } = string.Empty;

    [StringLength(240)]
    public string? PayPalClientId { get; set; } = string.Empty;

    [StringLength(240)]
    public string? PayPalClientSecret { get; set; } = string.Empty;

    public bool ClearPayPalClientSecret { get; set; }

    [StringLength(40)]
    public string? PayPalMode { get; set; } = "Sandbox";

    public bool HasPayPalClientSecret { get; set; }

    [StringLength(240)]
    public string? StripePublishableKey { get; set; } = string.Empty;

    [StringLength(240)]
    public string? StripeSecretKey { get; set; } = string.Empty;

    public bool ClearStripeSecretKey { get; set; }

    [StringLength(240)]
    public string? StripeWebhookSecret { get; set; } = string.Empty;

    public bool ClearStripeWebhookSecret { get; set; }

    public bool HasStripeSecretKey { get; set; }

    public bool HasStripeWebhookSecret { get; set; }

    [Range(0, 1)]
    public decimal BookingTaxFeeRate { get; set; } = 0.08m;

    [StringLength(80)]
    public string? BookingCouponCode { get; set; } = "VIETNAM10";

    [Range(0, 1)]
    public decimal BookingDiscountRate { get; set; } = 0.10m;

    [StringLength(12)]
    public string PaymentCurrency { get; set; } = "USD";

    [StringLength(240)]
    public string? CurrentPassword { get; set; } = string.Empty;

    [EmailAddress, StringLength(160)]
    public string? CurrentAdminEmail { get; set; } = string.Empty;

    [EmailAddress, StringLength(160)]
    public string? NewAdminEmail { get; set; } = string.Empty;

    [Compare(nameof(NewAdminEmail))]
    public string? ConfirmAdminEmail { get; set; } = string.Empty;

    [StringLength(240)]
    public string? AdminEmailCurrentPassword { get; set; } = string.Empty;

    [StringLength(240)]
    public string? NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword))]
    public string? ConfirmPassword { get; set; } = string.Empty;
}
