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
    [Route("blog")]
    [HttpGet]
    public async Task<ActionResult> Blog(string? status = "all", string? culture = "en", string? search = null, string? sort = "newest")
    {
        status = string.IsNullOrWhiteSpace(status) ? "all" : status.ToLowerInvariant();
        culture = NormalizeCulture(culture);
        sort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort.ToLowerInvariant();

        var now = DateTime.UtcNow;
        var query = _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.Translations)
            .AsNoTracking()
            .AsQueryable();

        query = status switch
        {
            "deleted" => query.Where(x => x.DeletedAtUtc != null),
            "draft" => query.Where(x => x.DeletedAtUtc == null && !x.IsPublished),
            "scheduled" => query.Where(x => x.DeletedAtUtc == null && x.IsPublished && x.PublishedAtUtc.HasValue && x.PublishedAtUtc > now),
            "published" => query.Where(x => x.DeletedAtUtc == null && x.IsPublished && (!x.PublishedAtUtc.HasValue || x.PublishedAtUtc <= now)),
            _ => query.Where(x => x.DeletedAtUtc == null)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Translations.Any(t =>
                t.Title.Contains(search) ||
                t.Slug.Contains(search) ||
                t.MetaDescription.Contains(search)));
        }

        query = sort == "published"
            ? query.OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            : query.OrderByDescending(x => x.UpdatedAtUtc);

        return View("Blog", new AdminBlogListViewModel
        {
            Status = status,
            Culture = culture,
            Search = search ?? string.Empty,
            Sort = sort,
            Posts = await query.Take(200).ToListAsync()
        });
    }

    [Route("blog/create")]
    [HttpGet]
    public async Task<ActionResult> CreateBlogPost(string? culture = "en")
    {
        ViewData["BlogFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = new BlogPostFormViewModel
        {
            Culture = NormalizeCulture(culture),
            PublishedAt = DateTime.Now,
            IsPublished = false
        };
        await PopulateBlogMediaAsync(model);
        return View("BlogForm", model);
    }

    [Route("blog/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateBlogPost(BlogPostFormViewModel model)
    {
        ViewData["BlogFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        NormalizeBlogForm(model);
        await ValidateBlogSlugAsync(model);
        if (!ModelState.IsValid)
        {
            await PopulateBlogMediaAsync(model);
            return View("BlogForm", model);
        }

        if (!await TryApplyUploadedBlogImagesAsync(model))
        {
            await PopulateBlogMediaAsync(model);
            return View("BlogForm", model);
        }

        var post = new BlogPost
        {
            IsPublished = model.IsPublished,
            PublishedAtUtc = ToUtcFromLocalOrNull(model.PublishedAt),
            FeaturedMediaAssetId = model.FeaturedMediaAssetId,
            OgMediaAssetId = model.OgMediaAssetId,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription,
            CanonicalUrl = model.CanonicalUrl,
            Robots = DefaultIfBlank(model.Robots, "index,follow"),
            OgTitle = model.OgTitle,
            OgDescription = model.OgDescription
        };
        ApplyBlogTranslation(post, model);
        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Blog post created.";
        return RedirectToAction(nameof(EditBlogPost), new { id = post.Id, culture = model.Culture });
    }

    [Route("blog/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditBlogPost(int id, string? culture = "en")
    {
        var post = await LoadBlogPostForEditAsync(id);
        if (post is null)
        {
            return HttpNotFound();
        }

        ViewData["BlogFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = BlogPostFormViewModel.FromEntity(post, NormalizeCulture(culture));
        await PopulateBlogMediaAsync(model);
        return View("BlogForm", model);
    }

    [Route("blog/{id:int}/preview")]
    [HttpGet]
    public async Task<ActionResult> PreviewBlogPost(int id, string? culture = "en")
    {
        var post = await _db.BlogPosts
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (post is null)
        {
            return HttpNotFound();
        }

        var model = ToAdminBlogPreviewModel(post, NormalizeCulture(culture));
        return View("~/Views/Home/BlogPost.cshtml", model);
    }

    [Route("blog/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditBlogPost(int id, BlogPostFormViewModel model)
    {
        ViewData["BlogFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var post = await LoadBlogPostForEditAsync(id);
        if (post is null)
        {
            return HttpNotFound();
        }

        NormalizeBlogForm(model);
        await ValidateBlogSlugAsync(model);
        if (!ModelState.IsValid)
        {
            await PopulateBlogMediaAsync(model);
            return View("BlogForm", model);
        }

        if (!await TryApplyUploadedBlogImagesAsync(model))
        {
            await PopulateBlogMediaAsync(model);
            return View("BlogForm", model);
        }

        post.IsPublished = model.IsPublished;
        post.PublishedAtUtc = ToUtcFromLocalOrNull(model.PublishedAt);
        post.UpdatedAtUtc = DateTime.UtcNow;
        post.FeaturedMediaAssetId = model.FeaturedMediaAssetId;
        post.OgMediaAssetId = model.OgMediaAssetId;
        post.MetaTitle = model.MetaTitle;
        post.MetaDescription = model.MetaDescription;
        post.CanonicalUrl = model.CanonicalUrl;
        post.Robots = DefaultIfBlank(model.Robots, "index,follow");
        post.OgTitle = model.OgTitle;
        post.OgDescription = model.OgDescription;
        ApplyBlogTranslation(post, model);

        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Blog post saved.";
        return RedirectToAction(nameof(EditBlogPost), new { id = post.Id, culture = model.Culture });
    }

    [Route("blog/{id:int}/unpublish")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UnpublishBlogPost(int id)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post is null || post.DeletedAtUtc is not null)
        {
            return HttpNotFound();
        }

        post.IsPublished = false;
        post.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Blog post moved to draft.";
        return RedirectToAction(nameof(Blog), new { status = "draft" });
    }

    [Route("blog/{id:int}/delete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteBlogPost(int id)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post is null || post.DeletedAtUtc is not null)
        {
            return HttpNotFound();
        }

        post.IsPublished = false;
        post.DeletedAtUtc = DateTime.UtcNow;
        post.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Blog post deleted.";
        return RedirectToAction(nameof(Blog), new { status = "all" });
    }

    [Route("pages")]
    [HttpGet]
    public async Task<ActionResult> Pages(string? status = "all", string? culture = "en", string? search = null, string? sort = "updated")
    {
        status = string.IsNullOrWhiteSpace(status) ? "all" : status.ToLowerInvariant();
        culture = NormalizeCulture(culture);
        sort = string.IsNullOrWhiteSpace(sort) ? "updated" : sort.ToLowerInvariant();

        var query = _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.Translations)
            .AsNoTracking()
            .AsQueryable();

        query = status switch
        {
            "deleted" => query.Where(x => x.DeletedAtUtc != null),
            "published" => query.Where(x => x.DeletedAtUtc == null && x.IsPublished),
            "draft" => query.Where(x => x.DeletedAtUtc == null && !x.IsPublished),
            _ => query.Where(x => x.DeletedAtUtc == null)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Slug.Contains(search) ||
                x.PublicPath.Contains(search) ||
                x.Title.Contains(search) ||
                x.Translations.Any(t => t.Title.Contains(search) || t.MetaDescription.Contains(search)));
        }

        query = sort == "title"
            ? query.OrderBy(x => x.Title)
            : query.OrderByDescending(x => x.UpdatedAtUtc);

        return View("Pages", new AdminCmsPageListViewModel
        {
            Pages = await query.Take(200).ToListAsync(),
            Status = status,
            Culture = culture,
            Search = search ?? string.Empty,
            Sort = sort
        });
    }

    [Route("pages/create")]
    [HttpGet]
    public async Task<ActionResult> CreatePage(string? culture = "en")
    {
        ViewData["CmsPageFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = new CmsPageFormViewModel
        {
            Culture = NormalizeCulture(culture),
            IsPublished = false,
            Robots = "index,follow"
        };
        await PopulateCmsPageMediaAsync(model);
        return View("PageForm", model);
    }

    [Route("pages/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePage(CmsPageFormViewModel model)
    {
        ViewData["CmsPageFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        NormalizeCmsPageForm(model);
        await ValidateCmsPageAsync(model, null);
        if (!ModelState.IsValid)
        {
            await PopulateCmsPageMediaAsync(model);
            return View("PageForm", model);
        }

        if (!await TryApplyUploadedCmsPageImagesAsync(model))
        {
            await PopulateCmsPageMediaAsync(model);
            return View("PageForm", model);
        }

        var page = new CmsPage
        {
            Slug = model.Slug,
            PublicPath = model.PublicPath,
            IsPublished = model.IsPublished,
            FeaturedMediaAssetId = model.FeaturedMediaAssetId,
            OgMediaAssetId = model.OgMediaAssetId,
            Title = model.Title,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription,
            CanonicalUrl = model.CanonicalUrl,
            Robots = DefaultIfBlank(model.Robots, "index,follow"),
            OgTitle = model.OgTitle,
            OgDescription = model.OgDescription,
            BodyHtml = model.BodyHtml ?? string.Empty
        };
        ApplyCmsPageTranslation(page, model);
        _db.CmsPages.Add(page);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "CMS page created.";
        return RedirectToAction(nameof(EditPage), new { id = page.Id, culture = model.Culture });
    }

    [Route("pages/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditPage(int id, string? culture = "en")
    {
        var page = await LoadCmsPageForEditAsync(id);
        if (page is null)
        {
            return HttpNotFound();
        }

        ViewData["CmsPageFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = CmsPageFormViewModel.FromEntity(page, NormalizeCulture(culture));
        await PopulateCmsPageMediaAsync(model);
        return View("PageForm", model);
    }

    [Route("pages/{id:int}/preview")]
    [HttpGet]
    public async Task<ActionResult> PreviewPage(int id, string? culture = "en")
    {
        var page = await _db.CmsPages
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.OgMediaAsset)
            .Include("Translations.OgMediaAsset")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (page is null)
        {
            return HttpNotFound();
        }

        return View("~/Views/Home/Page.cshtml", ToCmsPageViewModel(page, NormalizeCulture(culture), Request));
    }

    [Route("pages/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditPage(int id, CmsPageFormViewModel model)
    {
        ViewData["CmsPageFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var page = await LoadCmsPageForEditAsync(id);
        if (page is null)
        {
            return HttpNotFound();
        }

        NormalizeCmsPageForm(model);
        await ValidateCmsPageAsync(model, page);
        if (!ModelState.IsValid)
        {
            await PopulateCmsPageMediaAsync(model);
            return View("PageForm", model);
        }

        if (!await TryApplyUploadedCmsPageImagesAsync(model))
        {
            await PopulateCmsPageMediaAsync(model);
            return View("PageForm", model);
        }

        if (!string.Equals(page.PublicPath, model.PublicPath, StringComparison.OrdinalIgnoreCase))
        {
            page.OldPaths = AddOldCmsPath(page.OldPaths, page.PublicPath);
        }

        page.Slug = model.Slug;
        page.PublicPath = model.PublicPath;
        page.IsPublished = model.IsPublished;
        page.UpdatedAtUtc = DateTime.UtcNow;
        page.FeaturedMediaAssetId = model.FeaturedMediaAssetId;
        page.OgMediaAssetId = model.OgMediaAssetId;
        page.Title = model.Title;
        page.MetaTitle = model.MetaTitle;
        page.MetaDescription = model.MetaDescription;
        page.CanonicalUrl = model.CanonicalUrl;
        page.Robots = DefaultIfBlank(model.Robots, "index,follow");
        page.OgTitle = model.OgTitle;
        page.OgDescription = model.OgDescription;
        page.BodyHtml = model.BodyHtml ?? string.Empty;
        ApplyCmsPageTranslation(page, model);

        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "CMS page saved.";
        return RedirectToAction(nameof(EditPage), new { id = page.Id, culture = model.Culture });
    }

    [Route("pages/{id:int}/unpublish")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UnpublishPage(int id)
    {
        var page = await _db.CmsPages.FindAsync(id);
        if (page is null || page.DeletedAtUtc is not null)
        {
            return HttpNotFound();
        }

        page.IsPublished = false;
        page.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "CMS page unpublished.";
        return RedirectToAction(nameof(Pages), new { status = "draft" });
    }

    [Route("pages/{id:int}/delete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeletePage(int id)
    {
        var page = await _db.CmsPages.FindAsync(id);
        if (page is null || page.DeletedAtUtc is not null)
        {
            return HttpNotFound();
        }

        page.IsPublished = false;
        page.DeletedAtUtc = DateTime.UtcNow;
        page.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "CMS page deleted.";
        return RedirectToAction(nameof(Pages), new { status = "all" });
    }

    [Route("redirects")]
    [HttpGet]
    public async Task<ActionResult> Redirects(string? status = "active", string? search = null)
    {
        status = string.IsNullOrWhiteSpace(status) ? "active" : status.ToLowerInvariant();
        var query = _db.RedirectRules.AsNoTracking().AsQueryable();

        query = status switch
        {
            "inactive" => query.Where(x => x.DeletedAtUtc == null && !x.IsActive),
            "deleted" => query.Where(x => x.DeletedAtUtc != null),
            "all" => query.Where(x => x.DeletedAtUtc == null),
            _ => query.Where(x => x.DeletedAtUtc == null && x.IsActive)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.SourcePath.Contains(search) || x.TargetUrl.Contains(search) || x.Notes.Contains(search));
        }

        return View("Redirects", new AdminRedirectListViewModel
        {
            Redirects = await query.OrderBy(x => x.SourcePath).Take(300).ToListAsync(),
            Status = status,
            Search = search ?? string.Empty
        });
    }

    [Route("redirects/create")]
    [HttpGet]
    public ActionResult CreateRedirect()
    {
        ViewData["RedirectFormMode"] = "Create";
        return View("RedirectForm", new RedirectRuleFormViewModel());
    }

    [Route("redirects/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateRedirect(RedirectRuleFormViewModel model)
    {
        ViewData["RedirectFormMode"] = "Create";
        NormalizeRedirectRuleForm(model);
        await ValidateRedirectRuleAsync(model);
        if (!ModelState.IsValid)
        {
            return View("RedirectForm", model);
        }

        var rule = new RedirectRule();
        model.ApplyTo(rule);
        _db.RedirectRules.Add(rule);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Redirect created.";
        return RedirectToAction(nameof(Redirects));
    }

    [Route("redirects/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditRedirect(int id)
    {
        var rule = await _db.RedirectRules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (rule is null)
        {
            return HttpNotFound();
        }

        ViewData["RedirectFormMode"] = "Edit";
        return View("RedirectForm", RedirectRuleFormViewModel.FromEntity(rule));
    }

    [Route("redirects/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditRedirect(int id, RedirectRuleFormViewModel model)
    {
        ViewData["RedirectFormMode"] = "Edit";
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var rule = await _db.RedirectRules.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (rule is null)
        {
            return HttpNotFound();
        }

        NormalizeRedirectRuleForm(model);
        await ValidateRedirectRuleAsync(model);
        if (!ModelState.IsValid)
        {
            return View("RedirectForm", model);
        }

        model.ApplyTo(rule);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Redirect saved.";
        return RedirectToAction(nameof(Redirects));
    }

    [Route("redirects/{id:int}/delete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteRedirect(int id)
    {
        var rule = await _db.RedirectRules.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (rule is null)
        {
            return HttpNotFound();
        }

        rule.IsActive = false;
        rule.DeletedAtUtc = DateTime.UtcNow;
        rule.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Redirect deleted.";
        return RedirectToAction(nameof(Redirects));
    }

}
