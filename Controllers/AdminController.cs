using LuxuryCar.Data;
using System.Web;
using LuxuryCar.Identity;
using LuxuryCar.Models;
using LuxuryCar.Services;
using LuxuryCar.ViewModels;
using LuxuryCar.Infrastructure;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Host.SystemWeb;
using System.Data.Entity;

namespace LuxuryCar.Controllers;

[AdminAuthorize(Roles = "Admin")]
[RoutePrefix("admin")]
public partial class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IEmailService _emailService;
    private readonly IAppSettingService _settings;

    private ApplicationSignInManager SignInManager => HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
    private ApplicationUserManager UserManager => HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

    public AdminController(ApplicationDbContext db, IMediaStorageService mediaStorage, IEmailService emailService, IAppSettingService settings)
    {
        _db = db;
        _mediaStorage = mediaStorage;
        _emailService = emailService;
        _settings = settings;
    }

    private async Task PopulateVehicleMediaAsync(CarVehicleTypeFormViewModel model)
    {
        model.AvailableMedia = await LoadAvailableMediaAsync(100);
    }

    private async Task PopulateAddonMediaAsync(CarBookingAddonFormViewModel model)
    {
        model.AvailableMedia = await LoadAvailableMediaAsync(100);
    }

    private async Task PopulatePopularRouteMediaAsync(PopularRouteFormViewModel model)
    {
        model.AvailableMedia = await LoadAvailableMediaAsync(100);
    }

    private async Task PopulateBlogMediaAsync(BlogPostFormViewModel model)
    {
        model.AvailableMedia = await LoadAvailableMediaAsync(120);
    }

    private async Task PopulateCmsPageMediaAsync(CmsPageFormViewModel model)
    {
        model.AvailableMedia = await LoadAvailableMediaAsync(120);
    }

    private Task<List<MediaAsset>> LoadAvailableMediaAsync(int take) =>
        _db.MediaAssets
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync();

    private async Task<CmsPage?> LoadCmsPageForEditAsync(int id)
    {
        return await _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
    }

    private async Task ValidateCmsPageAsync(CmsPageFormViewModel model, CmsPage? currentPage)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug is required.");
        }

        if (string.IsNullOrWhiteSpace(model.PublicPath))
        {
            ModelState.AddModelError(nameof(model.PublicPath), "Public path is required.");
            return;
        }

        if (IsReservedCmsPath(model.PublicPath))
        {
            ModelState.AddModelError(nameof(model.PublicPath), "This path is reserved for system routes.");
            return;
        }

        var currentId = currentPage?.Id ?? 0;
        var slugExists = await _db.CmsPages.AnyAsync(x => x.Id != currentId && x.Slug == model.Slug);
        if (slugExists)
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already used.");
        }

        var pathExists = await _db.CmsPages.AnyAsync(x => x.Id != currentId && x.PublicPath == model.PublicPath);
        if (pathExists)
        {
            ModelState.AddModelError(nameof(model.PublicPath), "This public path is already used.");
        }

        var oldPathConflict = await _db.CmsPages
            .AsNoTracking()
            .Where(x => x.Id != currentId && x.OldPaths.Contains(model.PublicPath))
            .ToListAsync();
        if (oldPathConflict.Any(x => CmsOldPaths(x.OldPaths).Contains(model.PublicPath, StringComparer.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.PublicPath), "This path is already reserved as an old redirect path.");
        }
    }

    private async Task ValidateRedirectRuleAsync(RedirectRuleFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SourcePath))
        {
            ModelState.AddModelError(nameof(model.SourcePath), "Source path is required.");
            return;
        }

        if (IsReservedCmsPath(model.SourcePath))
        {
            ModelState.AddModelError(nameof(model.SourcePath), "Source path is reserved for system routes.");
            return;
        }

        if (string.IsNullOrWhiteSpace(model.TargetUrl))
        {
            ModelState.AddModelError(nameof(model.TargetUrl), "Target URL is required.");
            return;
        }

        if (string.Equals(model.SourcePath, model.TargetUrl, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.TargetUrl), "Target URL cannot be the same as source path.");
        }

        var exists = await _db.RedirectRules.AnyAsync(x =>
            x.Id != model.Id &&
            x.DeletedAtUtc == null &&
            x.SourcePath == model.SourcePath);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.SourcePath), "This source path already has a redirect rule.");
        }
    }

    private async Task<bool> TryApplyUploadedCmsPageImagesAsync(CmsPageFormViewModel model)
    {
        try
        {
            if (model.FeaturedImageFile != null && model.FeaturedImageFile.ContentLength > 0)
            {
                var asset = await CreateMediaAssetFromUploadAsync(model.FeaturedImageFile, model.FeaturedImageAltText);
                model.FeaturedMediaAssetId = asset.Id;
                model.CurrentFeaturedImageUrl = asset.SecureUrl;
            }

            if (model.OgImageFile != null && model.OgImageFile.ContentLength > 0)
            {
                var asset = await CreateMediaAssetFromUploadAsync(model.OgImageFile, model.OgImageAltText);
                model.OgMediaAssetId = asset.Id;
                model.CurrentOgImageUrl = asset.SecureUrl;
            }

            return true;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
    }

    private void ApplyCmsPageTranslation(CmsPage page, CmsPageFormViewModel model)
    {
        var translation = page.Translations.FirstOrDefault(x => x.Culture == model.Culture);
        if (translation is null)
        {
            translation = new CmsPageTranslation { Culture = model.Culture };
            page.Translations.Add(translation);
        }

        translation.Title = model.Title;
        translation.BodyHtml = model.BodyHtml ?? string.Empty;
        translation.MetaTitle = model.MetaTitle;
        translation.MetaDescription = model.MetaDescription;
        translation.CanonicalUrl = model.CanonicalUrl;
        translation.Robots = DefaultIfBlank(model.Robots, "index,follow");
        translation.OgTitle = model.OgTitle;
        translation.OgDescription = model.OgDescription;
        translation.OgMediaAssetId = model.OgMediaAssetId;
        translation.UpdatedAtUtc = DateTime.UtcNow;
    }

    private CmsPageViewModel ToCmsPageViewModel(CmsPage page, string culture, HttpRequestBase request)
    {
        var translation = page.Translations.FirstOrDefault(x => x.Culture == culture)
            ?? page.Translations.FirstOrDefault(x => x.Culture == "en")
            ?? page.Translations.FirstOrDefault()
            ?? new CmsPageTranslation();
        var canonical = string.IsNullOrWhiteSpace(translation.CanonicalUrl)
            ? page.CanonicalUrl
            : translation.CanonicalUrl;
        if (string.IsNullOrWhiteSpace(canonical))
        {
            var baseUrl = request.Url?.GetLeftPart(UriPartial.Authority) ?? string.Empty;
            canonical = $"{baseUrl}{page.PublicPath}";
        }

        var ogMedia = translation.OgMediaAsset ?? page.OgMediaAsset ?? page.FeaturedMediaAsset;
        return new CmsPageViewModel
        {
            Page = page,
            Translation = translation,
            Slug = page.Slug,
            PublicPath = page.PublicPath,
            Culture = translation.Culture,
            Title = FirstNonBlank(translation.Title, page.Title),
            MetaTitle = FirstNonBlank(translation.MetaTitle, page.MetaTitle, translation.Title, page.Title),
            MetaDescription = FirstNonBlank(translation.MetaDescription, page.MetaDescription),
            CanonicalUrl = canonical,
            Robots = FirstNonBlank(translation.Robots, page.Robots, "index,follow"),
            OgTitle = FirstNonBlank(translation.OgTitle, page.OgTitle, translation.Title, page.Title),
            OgDescription = FirstNonBlank(translation.OgDescription, page.OgDescription, translation.MetaDescription, page.MetaDescription),
            OgImageUrl = ogMedia?.SecureUrl ?? string.Empty,
            BodyHtml = string.IsNullOrWhiteSpace(translation.BodyHtml) ? page.BodyHtml : translation.BodyHtml
        };
    }

    private async Task<BlogPost?> LoadBlogPostForEditAsync(int id)
    {
        return await _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
    }

    private async Task ValidateBlogSlugAsync(BlogPostFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug is required.");
            return;
        }

        var exists = await _db.BlogPostTranslations.AnyAsync(x =>
            x.Culture == model.Culture &&
            x.Slug == model.Slug &&
            x.BlogPostId != model.Id);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already used for the selected language.");
        }
    }

    private async Task<bool> TryApplyUploadedBlogImagesAsync(BlogPostFormViewModel model)
    {
        try
        {
            if (model.FeaturedImageFile != null && model.FeaturedImageFile.ContentLength > 0)
            {
                var asset = await CreateMediaAssetFromUploadAsync(model.FeaturedImageFile, model.FeaturedImageAltText);
                model.FeaturedMediaAssetId = asset.Id;
                model.CurrentFeaturedImageUrl = asset.SecureUrl;
            }

            if (model.OgImageFile != null && model.OgImageFile.ContentLength > 0)
            {
                var asset = await CreateMediaAssetFromUploadAsync(model.OgImageFile, model.OgImageAltText);
                model.OgMediaAssetId = asset.Id;
                model.CurrentOgImageUrl = asset.SecureUrl;
            }

            return true;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return false;
        }
    }

    private void ApplyBlogTranslation(BlogPost post, BlogPostFormViewModel model)
    {
        var translation = post.Translations.FirstOrDefault(x => x.Culture == model.Culture);
        if (translation is null)
        {
            translation = new BlogPostTranslation { Culture = model.Culture };
            post.Translations.Add(translation);
        }

        translation.Slug = model.Slug;
        translation.Title = model.Title;
        translation.Excerpt = model.Excerpt;
        translation.BodyHtml = model.BodyHtml ?? string.Empty;
        translation.MetaTitle = model.MetaTitle;
        translation.MetaDescription = model.MetaDescription;
        translation.CanonicalUrl = model.CanonicalUrl;
        translation.Robots = DefaultIfBlank(model.Robots, "index,follow");
        translation.OgTitle = model.OgTitle;
        translation.OgDescription = model.OgDescription;
        translation.OgMediaAssetId = model.OgMediaAssetId;
        translation.UpdatedAtUtc = DateTime.UtcNow;
    }

    private BlogPostViewModel ToAdminBlogPreviewModel(BlogPost post, string culture)
    {
        var translation = post.Translations.FirstOrDefault(x => x.Culture == culture)
            ?? post.Translations.FirstOrDefault(x => x.Culture == "en")
            ?? post.Translations.FirstOrDefault()
            ?? new BlogPostTranslation();
        var canonical = string.IsNullOrWhiteSpace(translation.CanonicalUrl)
            ? post.CanonicalUrl
            : translation.CanonicalUrl;
        if (string.IsNullOrWhiteSpace(canonical) && !string.IsNullOrWhiteSpace(translation.Slug))
        {
            canonical = $"{Request.Url?.Scheme ?? "https"}://{Request.Url?.Authority}/blog/{translation.Slug}";
        }

        var ogMedia = translation.OgMediaAsset ?? post.OgMediaAsset ?? post.FeaturedMediaAsset;
        return new BlogPostViewModel
        {
            Post = post,
            Translation = translation,
            MetaTitle = FirstNonBlank(translation.MetaTitle, post.MetaTitle, translation.Title),
            MetaDescription = FirstNonBlank(translation.MetaDescription, post.MetaDescription, translation.Excerpt),
            CanonicalUrl = canonical,
            Robots = FirstNonBlank(translation.Robots, post.Robots, "noindex,nofollow"),
            OgTitle = FirstNonBlank(translation.OgTitle, post.OgTitle, translation.Title),
            OgDescription = FirstNonBlank(translation.OgDescription, post.OgDescription, translation.Excerpt),
            OgImageUrl = ogMedia?.SecureUrl ?? string.Empty
        };
    }

    private static string FirstNonBlank(params string[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

    private static void NormalizeBlogForm(BlogPostFormViewModel model)
    {
        model.Culture = NormalizeCulture(model.Culture);
        model.Slug = Slugify(model.Slug);
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Excerpt = model.Excerpt?.Trim() ?? string.Empty;
        model.MetaTitle = model.MetaTitle?.Trim() ?? string.Empty;
        model.MetaDescription = model.MetaDescription?.Trim() ?? string.Empty;
        model.CanonicalUrl = model.CanonicalUrl?.Trim() ?? string.Empty;
        model.Robots = DefaultIfBlank(model.Robots, "index,follow");
        model.OgTitle = model.OgTitle?.Trim() ?? string.Empty;
        model.OgDescription = model.OgDescription?.Trim() ?? string.Empty;
    }

    private static void NormalizeCmsPageForm(CmsPageFormViewModel model)
    {
        model.Culture = NormalizeCulture(model.Culture);
        model.Slug = Slugify(model.Slug);
        model.PublicPath = NormalizePublicPath(model.PublicPath, model.Slug);
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.MetaTitle = model.MetaTitle?.Trim() ?? string.Empty;
        model.MetaDescription = model.MetaDescription?.Trim() ?? string.Empty;
        model.CanonicalUrl = model.CanonicalUrl?.Trim() ?? string.Empty;
        model.Robots = DefaultIfBlank(model.Robots, "index,follow");
        model.OgTitle = model.OgTitle?.Trim() ?? string.Empty;
        model.OgDescription = model.OgDescription?.Trim() ?? string.Empty;
    }

    private static void NormalizeRedirectRuleForm(RedirectRuleFormViewModel model)
    {
        model.SourcePath = NormalizePublicPath(model.SourcePath, string.Empty);
        model.TargetUrl = NormalizeRedirectTarget(model.TargetUrl);
        model.StatusCode = model.StatusCode is 302 ? 302 : 301;
        model.Notes = model.Notes?.Trim() ?? string.Empty;
    }

    private static string NormalizeCulture(string? culture)
    {
        var value = string.IsNullOrWhiteSpace(culture) ? "en" : culture.Trim().ToLowerInvariant();
        return value is "en" or "vi" or "ko" or "zh" or "ja" or "fr" ? value : "en";
    }

    private static string Slugify(string value)
    {
        value = (value ?? string.Empty).Trim().ToLowerInvariant();
        value = System.Text.RegularExpressions.Regex.Replace(value, @"[^a-z0-9\-\s]", "");
        value = System.Text.RegularExpressions.Regex.Replace(value, @"[\s\-]+", "-");
        return value.Trim('-');
    }

    private static string NormalizePublicPath(string? path, string slug)
    {
        var value = (path ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = $"/{slug}";
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            value = uri.AbsolutePath;
        }

        if (!value.StartsWith("/"))
        {
            value = "/" + value;
        }

        value = value.Split(new[] { '?', '#' })[0].TrimEnd('/');
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var parts = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Slugify(x))
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return "/" + string.Join("/", parts);
    }

    private static string NormalizeRedirectTarget(string? target)
    {
        var value = (target ?? string.Empty).Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return uri.ToString();
        }

        return NormalizePublicPath(value, string.Empty);
    }

    private static bool IsReservedCmsPath(string path)
    {
        if (string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var reserved = new[] { "/admin", "/blog", "/booking", "/payment", "/contact", "/culture", "/css", "/js", "/images", "/lib", "/api", "/home" };
        return reserved.Any(prefix =>
            string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static string AddOldCmsPath(string oldPaths, string path)
    {
        path = NormalizePublicPath(path, string.Empty);
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return oldPaths ?? string.Empty;
        }

        var paths = CmsOldPaths(oldPaths).ToList();
        if (!paths.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
        {
            paths.Add(path);
        }

        return string.Join("\n", paths);
    }

    private static IEnumerable<string> CmsOldPaths(string? oldPaths) =>
        (oldPaths ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Select(x => NormalizePublicPath(x, string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "/")
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static DateTime? ToUtcFromLocalOrNull(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime();
    }

    private async Task<bool> TryApplyUploadedVehicleImageAsync(CarVehicleTypeFormViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.ContentLength == 0)
        {
            return true;
        }

        try
        {
            var asset = await CreateMediaAssetFromUploadAsync(model.ImageFile, model.ImageAltText);
            model.MediaAssetId = asset.Id;
            model.CurrentImageUrl = asset.SecureUrl;
            return true;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return false;
        }
    }

    private static bool IsLocalMediaAsset(MediaAsset asset)
    {
        return asset.PublicId.StartsWith("local/", StringComparison.OrdinalIgnoreCase)
            || asset.SecureUrl.StartsWith("/", StringComparison.OrdinalIgnoreCase)
            || asset.Url.StartsWith("/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> TryApplyUploadedAddonImageAsync(CarBookingAddonFormViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.ContentLength == 0)
        {
            return true;
        }

        try
        {
            var asset = await CreateMediaAssetFromUploadAsync(model.ImageFile, model.ImageAltText);
            model.MediaAssetId = asset.Id;
            model.CurrentImageUrl = asset.SecureUrl;
            return true;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return false;
        }
    }

    private async Task<bool> TryApplyUploadedPopularRouteImageAsync(PopularRouteFormViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.ContentLength == 0)
        {
            return true;
        }

        try
        {
            var asset = await CreateMediaAssetFromUploadAsync(model.ImageFile, model.ImageAltText);
            model.MediaAssetId = asset.Id;
            model.CurrentImageUrl = asset.SecureUrl;
            return true;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return false;
        }
    }

    private async Task<MediaAsset> CreateMediaAssetFromUploadAsync(HttpPostedFileBase file, string altText)
    {
        var upload = await _mediaStorage.UploadImageAsync(file, altText);
        var asset = new MediaAsset
        {
            PublicId = upload.PublicId,
            Url = upload.Url,
            SecureUrl = upload.SecureUrl,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            Bytes = upload.Bytes,
            Width = upload.Width,
            Height = upload.Height,
            AltText = altText?.Trim() ?? string.Empty,
            Folder = upload.Folder
        };
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync();
        return asset;
    }

    private async Task ValidateAirportCodeAsync(AirportFormViewModel model)
    {
        model.Code = model.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            return;
        }

        var exists = await _db.Airports.AnyAsync(x => x.Id != model.Id && x.Code == model.Code);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Code), "Airport code already exists.");
        }
    }

    private async Task PopulateBookingEditOptionsAsync(AdminBookingEditViewModel model)
    {
        model.Airports = await _db.Airports
            .AsNoTracking()
            .OrderBy(x => x.City)
            .ThenBy(x => x.Code)
            .Select(x => new SelectListItem { Text = x.City + " - " + x.Name + " (" + x.Code + ")", Value = x.Id.ToString(), Selected = model.AirportId == x.Id })
            .ToListAsync();

        model.Vehicles = await _db.CarVehicleTypes
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString(), Selected = model.CarVehicleTypeId == x.Id })
            .ToListAsync();
    }

    private static DateTime ToVietnamTime(DateTime utcDateTime)
    {
        var zone = GetVietnamTimeZone();
        var source = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(source, zone);
    }

    private static DateTime ToUtcFromVietnamTime(DateTime localDateTime)
    {
        var zone = GetVietnamTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), zone);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private async Task<AdminSettingsViewModel> BuildSettingsModelAsync()
    {
        var userId = User.Identity.GetUserId();
        var currentUser = string.IsNullOrWhiteSpace(userId) ? null : await UserManager.FindByIdAsync(userId);
        return new AdminSettingsViewModel
        {
            SiteBrandName = await _settings.GetAsync("Site:BrandName", "Vietnam Transfer"),
            SiteTagline = await _settings.GetAsync("Site:Tagline", "Private Transfer"),
            SiteHotline = await _settings.GetAsync("Site:Hotline", "1900 8888"),
            SiteContactEmail = await _settings.GetAsync("Site:ContactEmail", "info@vietnamtransfer.vn"),
            SiteAddress = await _settings.GetAsync("Site:Address", "123 Nguyen Hue, District 1, Ho Chi Minh City"),
            ThemeInkColor = NormalizeHexColor(await _settings.GetAsync("Theme:Ink", "#151515"), "#151515"),
            ThemeMutedColor = NormalizeHexColor(await _settings.GetAsync("Theme:Muted", "#6f6f6f"), "#6f6f6f"),
            ThemeAccentColor = NormalizeHexColor(await _settings.GetAsync("Theme:Accent", "#c89b3c"), "#c89b3c"),
            ThemeAccentDarkColor = NormalizeHexColor(await _settings.GetAsync("Theme:AccentDark", "#a87920"), "#a87920"),
            ThemePaperColor = NormalizeHexColor(await _settings.GetAsync("Theme:Paper", "#ffffff"), "#ffffff"),
            ThemeSoftColor = NormalizeHexColor(await _settings.GetAsync("Theme:Soft", "#f7f5f0"), "#f7f5f0"),
            ThemeLineColor = NormalizeHexColor(await _settings.GetAsync("Theme:Line", "#e9e4d9"), "#e9e4d9"),
            EmailSmtpHost = await _settings.GetAsync("Email:SmtpHost"),
            EmailSmtpPort = ParsePort(await _settings.GetAsync("Email:SmtpPort", "587")),
            EmailEnableSsl = await _settings.GetBoolAsync("Email:EnableSsl", true),
            EmailUsername = await _settings.GetAsync("Email:Username"),
            EmailFrom = await _settings.GetAsync("Email:From", "bookings@vietnamtransfer.local"),
            HasEmailPassword = !string.IsNullOrWhiteSpace(await _settings.GetAsync("Email:Password")),
            IsEmailConfigured = !string.IsNullOrWhiteSpace(await _settings.GetAsync("Email:SmtpHost")),
            CloudinaryCloudName = await _settings.GetAsync("Cloudinary:CloudName"),
            CloudinaryApiKey = await _settings.GetAsync("Cloudinary:ApiKey"),
            CloudinaryFolder = await _settings.GetAsync("Cloudinary:Folder", "vietnamtransfer"),
            HasCloudinaryApiSecret = !string.IsNullOrWhiteSpace(await _settings.GetAsync("Cloudinary:ApiSecret")),
            IsCloudinaryConfigured = await _mediaStorage.IsConfiguredAsync(),
            GeoapifyApiKey = await _settings.GetAsync("Geoapify:ApiKey"),
            PayPalClientId = await _settings.GetAsync("PayPal:ClientId"),
            PayPalMode = await _settings.GetAsync("PayPal:Mode", "Sandbox"),
            HasPayPalClientSecret = !string.IsNullOrWhiteSpace(await _settings.GetAsync("PayPal:ClientSecret")),
            StripePublishableKey = await _settings.GetAsync("Stripe:PublishableKey"),
            HasStripeSecretKey = !string.IsNullOrWhiteSpace(await _settings.GetAsync("Stripe:SecretKey")),
            HasStripeWebhookSecret = !string.IsNullOrWhiteSpace(await _settings.GetAsync("Stripe:WebhookSecret")),
            BookingTaxFeeRate = await _settings.GetDecimalAsync("Booking:TaxFeeRate", 0.08m),
            BookingCouponCode = await _settings.GetAsync("Booking:CouponCode", "VIETNAM10"),
            BookingDiscountRate = await _settings.GetDecimalAsync("Booking:DiscountRate", 0.10m),
            PaymentCurrency = await _settings.GetAsync("Payment:Currency", "USD"),
            CurrentAdminEmail = currentUser?.Email ?? currentUser?.UserName ?? string.Empty
        };
    }

    private async Task SaveSecretAsync(string key, string? newValue, bool clear)
    {
        if (clear)
        {
            await _settings.SetAsync(key, string.Empty);
            return;
        }

        if (!string.IsNullOrWhiteSpace(newValue))
        {
            await _settings.SetAsync(key, newValue);
        }
    }

    private static string DefaultIfBlank(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

    private static string NormalizeHexColor(string? value, string defaultValue)
    {
        var color = value?.Trim();
        if (color?.Length == 7 &&
            color[0] == '#' &&
            color.Skip(1).All(Uri.IsHexDigit))
        {
            return color.ToLowerInvariant();
        }

        return defaultValue;
    }

    private static int ParsePort(string value) =>
        int.TryParse(value, out var port) ? Clamp(port, 1, 65535) : 587;

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : value > max ? max : value;

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;

}
