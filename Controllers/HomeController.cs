using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Services;
using LuxuryCar.ViewModels;
using System.Web.Mvc;
using System.Web;
using System.Data.Entity;
using System.Globalization;
using System.Diagnostics;
using LuxuryCar.Models;

namespace LuxuryCar.Controllers;

public class HomeController : Controller
{
    private readonly IAppLogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IAppSettingService _settings;

    public HomeController(IAppLogger<HomeController> logger, ApplicationDbContext db, IAppSettingService settings)
    {
        _logger = logger;
        _db = db;
        _settings = settings;
    }

    [Route("")]
    [Route("Home/Index")]
    public async Task<ActionResult> Index()
    {
        var airports = await _db.Airports
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.City)
            .ToListAsync();

        var model = new HomeViewModel
        {
            Airports = airports,
            PopularDestinations = airports.Take(6).ToList(),
            CarVehicleTypes = await _db.CarVehicleTypes
                .AsNoTracking()
                .Include(x => x.Image)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(),
            FeaturedAddons = await _db.CarBookingAddons
                .AsNoTracking()
                .Include(x => x.Image)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Take(6)
                .ToListAsync(),
            PopularRoutes = await _db.PopularRoutes
                .AsNoTracking()
                .Include(x => x.Image)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Title)
                .Take(12)
                .ToListAsync(),
            GeoapifyApiKey = await _settings.GetAsync("Geoapify:ApiKey"),
            FeaturedReviews = await _db.BookingReviews
                .AsNoTracking()
                .Include(x => x.Booking)
                .Where(x => x.Rating == 5 && x.Comment != "")
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(4)
                .ToListAsync()
                };
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var latestPosts = await _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished && (!x.PublishedAtUtc.HasValue || x.PublishedAtUtc <= DateTime.UtcNow))
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Take(2)
            .ToListAsync();
        model.LatestBlogPosts = latestPosts.Select(x => ToBlogPostViewModel(x, culture, Request)).ToList();
        return View(model);
        }
    [Route("pages/{slug}")]
    public async Task<ActionResult> Page(string slug)
    {
        var blogRedirect = await _db.BlogPostTranslations
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Where(x => x.BlogPost.DeletedAtUtc == null)
            .Select(x => x.Slug)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(blogRedirect))
        {
            return RedirectPermanent($"/blog/{blogRedirect}");
        }

        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var requestedPath = $"/pages/{slug}".TrimEnd('/');
        var redirectRule = await TryRedirectRuleAsync(requestedPath);
        if (redirectRule is not null)
        {
            return redirectRule;
        }

        var pageByPublicPath = await _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicPath == requestedPath && x.DeletedAtUtc == null && x.IsPublished);
        if (pageByPublicPath is not null)
        {
            return View(ToCmsPageViewModel(pageByPublicPath, culture, Request));
        }

        var possibleRedirects = await _db.CmsPages
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished && x.OldPaths.Contains(requestedPath))
            .ToListAsync();
        var redirectPage = possibleRedirects.FirstOrDefault(x => CmsOldPaths(x.OldPaths).Contains(requestedPath, StringComparer.OrdinalIgnoreCase));
        if (redirectPage is not null)
        {
            return RedirectPermanent(redirectPage.PublicPath);
        }

        var page = await _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.DeletedAtUtc == null && x.IsPublished);
        if (page is null)
        {
            return HttpNotFound();
        }

        var legacyPath = $"/pages/{slug}";
        if (!string.Equals(page.PublicPath, legacyPath, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(page.PublicPath);
        }

        return View(ToCmsPageViewModel(page, culture, Request));
    }

    [Route("blog")]
    public async Task<ActionResult> Blog(string? search)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var query = _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished && (!x.PublishedAtUtc.HasValue || x.PublishedAtUtc <= DateTime.UtcNow));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Translations.Any(t => t.Title.Contains(search) || t.Excerpt.Contains(search)));
        }

        var posts = await query
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Take(24)
            .ToListAsync();

        var featured = await _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished)
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Take(4)
            .ToListAsync();

        return View("BlogIndex", new BlogIndexViewModel
        {
            Posts = posts.Select(x => ToBlogPostViewModel(x, culture, Request)).Where(x => !string.IsNullOrWhiteSpace(x.Translation.Slug)).ToList(),
            FeaturedPosts = featured.Select(x => ToBlogPostViewModel(x, culture, Request)).Where(x => !string.IsNullOrWhiteSpace(x.Translation.Slug)).ToList(),
            Search = search ?? string.Empty
        });
    }

    [Route("blog/{slug}")]
    public async Task<ActionResult> BlogPost(string slug)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var post = await _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished && (!x.PublishedAtUtc.HasValue || x.PublishedAtUtc <= DateTime.UtcNow))
            .FirstOrDefaultAsync(x => x.Translations.Any(t => t.Slug == slug));
        if (post is null)
        {
            return HttpNotFound();
        }

        var model = ToBlogPostViewModel(post, culture, Request);
        if (string.IsNullOrWhiteSpace(model.Translation.Slug))
        {
            return HttpNotFound();
        }

        if (!string.Equals(model.Translation.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent($"/blog/{model.Translation.Slug}");
        }

        return View("BlogPost", model);
    }

    [Route("hire")]
    [HttpGet]
    public async Task<ActionResult> Hire(CarHireViewModel model)
    {
        model.Vehicles = await _db.CarVehicleTypes
            .AsNoTracking()
            .Include(x => x.Image)
            .Where(x => x.IsActive && x.DailyRateUsd > 0)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        model.VehicleOptions = model.Vehicles
            .Select(x => new System.Web.Mvc.SelectListItem
            {
                Text = $"{x.Name} - ${x.DailyRateUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}/ngày",
                Value = x.Id.ToString(),
                Selected = model.CarVehicleTypeId == x.Id
            }).ToList();

        if (model.CarVehicleTypeId.HasValue)
        {
            model.SelectedVehicle = model.Vehicles
                .FirstOrDefault(x => x.Id == model.CarVehicleTypeId.Value);
        }

        model.GeoapifyApiKey = await _settings.GetAsync("Geoapify:ApiKey");

        return View("Hire", model);
    }

    [Route("routes/{slug}")]
    public async Task<ActionResult> RouteDetail(string slug)
    {
        var route = await _db.PopularRoutes
            .Include(x => x.Image)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);

        if (route is null)
        {
            return HttpNotFound();
        }

        var airports = await _db.Airports
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.City)
            .ToListAsync();

        ViewBag.Airports = airports;
        return View("RouteDetail", route);
    }

    [Route("Home/Privacy")]
    [Route("Home/Privacy/{*path}")]
    public ActionResult Privacy()
    {
        return Redirect("/privacy");
    }

    [Route("contact")]
    public ActionResult Contact()
    {
        return View();
    }

    public ActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        return View();
    }

    [Route("Home/HttpStatus")]
    public ActionResult HttpStatus(int code)
    {
        Response.StatusCode = code;

        if (code == 404)
        {
            return View("NotFound");
        }

        ViewBag.StatusCode = code;
        ViewBag.RequestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        return View("Error");
    }

    private static BlogPostViewModel ToBlogPostViewModel(BlogPost post, string culture, HttpRequestBase request)
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
            var baseUrl = request.Url?.GetLeftPart(UriPartial.Authority) ?? string.Empty;
            canonical = $"{baseUrl}/blog/{translation.Slug}";
        }

        var ogMedia = translation.OgMediaAsset ?? post.OgMediaAsset ?? post.FeaturedMediaAsset;
        return new BlogPostViewModel
        {
            Post = post,
            Translation = translation,
            MetaTitle = FirstNonBlank(translation.MetaTitle, post.MetaTitle, translation.Title),
            MetaDescription = FirstNonBlank(translation.MetaDescription, post.MetaDescription, translation.Excerpt),
            CanonicalUrl = canonical,
            Robots = FirstNonBlank(translation.Robots, post.Robots, "index,follow"),
            OgTitle = FirstNonBlank(translation.OgTitle, post.OgTitle, translation.Title),
            OgDescription = FirstNonBlank(translation.OgDescription, post.OgDescription, translation.Excerpt),
            OgImageUrl = ogMedia?.SecureUrl ?? string.Empty
        };
    }

    private static string FirstNonBlank(params string[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

    public async Task<ActionResult> CmsPath(string? path)
    {
        var publicPath = NormalizeCmsPath(path);
        if (IsReservedCmsPath(publicPath))
        {
            return HttpNotFound();
        }

        var redirectRule = await TryRedirectRuleAsync(publicPath);
        if (redirectRule is not null)
        {
            return redirectRule;
        }

        var page = await _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DeletedAtUtc == null && x.IsPublished && x.PublicPath == publicPath);

        if (page is not null)
        {
            return View("Page", ToCmsPageViewModel(page, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Request));
        }

        var possibleRedirects = await _db.CmsPages
            .Include(x => x.Translations)
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null && x.IsPublished && x.OldPaths.Contains(publicPath))
            .ToListAsync();
        var redirectPage = possibleRedirects.FirstOrDefault(x => CmsOldPaths(x.OldPaths).Contains(publicPath, StringComparer.OrdinalIgnoreCase));
        if (redirectPage is not null)
        {
            return RedirectPermanent(redirectPage.PublicPath);
        }

        return HttpNotFound();
    }

    private static CmsPageViewModel ToCmsPageViewModel(CmsPage page, string culture, HttpRequestBase request)
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

    private static string NormalizeCmsPath(string? path)
    {
        var value = "/" + (path ?? string.Empty).Trim('/');
        return value == "/" ? "/" : value.TrimEnd('/');
    }

    private async Task<ActionResult?> TryRedirectRuleAsync(string sourcePath)
    {
        var rule = await _db.RedirectRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.DeletedAtUtc == null &&
                x.IsActive &&
                x.SourcePath == sourcePath);

        if (rule is null)
        {
            return null;
        }

        return rule.StatusCode == 302
            ? Redirect(rule.TargetUrl)
            : RedirectPermanent(rule.TargetUrl);
    }

    private static bool IsReservedCmsPath(string path)
    {
        if (string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var reserved = new[] { "/admin", "/account", "/blog", "/booking", "/payment", "/contact", "/culture", "/css", "/js", "/images", "/lib", "/api", "/home" };
        return reserved.Any(prefix =>
            string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> CmsOldPaths(string? oldPaths) =>
        (oldPaths ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Select(NormalizeCmsPath)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "/")
            .Distinct(StringComparer.OrdinalIgnoreCase);
}