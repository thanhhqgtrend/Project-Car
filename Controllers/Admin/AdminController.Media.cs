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
    [Route("media")]
    [HttpGet]
    public async Task<ActionResult> Media(string? search = null)
    {
        var query = _db.MediaAssets.AsNoTracking().Where(x => x.DeletedAtUtc == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FileName.Contains(search) || x.AltText.Contains(search) || x.PublicId.Contains(search));
        }

        var model = new AdminMediaListViewModel
        {
            Search = search ?? string.Empty,
            IsCloudinaryConfigured = await _mediaStorage.IsConfiguredAsync(),
            Assets = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync()
        };
        return View(model);
    }

    [Route("media/upload")]
    [HttpGet]
    public async Task<ActionResult> UploadMedia()
    {
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        return View(new MediaUploadViewModel());
    }

    [Route("media/upload")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UploadMedia(MediaUploadViewModel model)
    {
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var asset = await CreateMediaAssetFromUploadAsync(model.File!, model.AltText);
            TempData["AdminNotice"] = "Media uploaded.";
            return RedirectToAction(nameof(MediaDetails), new { id = asset.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Route("media/{id:int}")]
    [HttpGet]
    public async Task<ActionResult> MediaDetails(int id)
    {
        var asset = await _db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        return asset is null ? HttpNotFound() : View(asset);
    }

    [Route("media/{id:int}/delete")]
    [HttpGet]
    public async Task<ActionResult> DeleteMedia(int id)
    {
        var asset = await _db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        return asset is null ? HttpNotFound() : View(asset);
    }

    [Route("media/{id:int}/delete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteMediaConfirmed(int id)
    {
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAtUtc == null);
        if (asset is null)
        {
            return HttpNotFound();
        }

        try
        {
            if (!IsLocalMediaAsset(asset))
            {
                await _mediaStorage.DeleteImageAsync(asset.PublicId);
            }
            asset.DeletedAtUtc = DateTime.UtcNow;
            var vehicles = await _db.CarVehicleTypes.Where(x => x.MediaAssetId == asset.Id).ToListAsync();
            foreach (var vehicle in vehicles)
            {
                vehicle.MediaAssetId = null;
            }
            var addons = await _db.CarBookingAddons.Where(x => x.MediaAssetId == asset.Id).ToListAsync();
            foreach (var addon in addons)
            {
                addon.MediaAssetId = null;
            }
            var popularRoutes = await _db.PopularRoutes.Where(x => x.MediaAssetId == asset.Id).ToListAsync();
            foreach (var route in popularRoutes)
            {
                route.MediaAssetId = null;
            }

            await _db.SaveChangesAsync();
            TempData["AdminNotice"] = IsLocalMediaAsset(asset) ? "Local media deleted." : "Media deleted from Cloudinary.";
            return RedirectToAction(nameof(Media));
        }
        catch (Exception ex)
        {
            TempData["AdminNotice"] = ex.Message;
            return RedirectToAction(nameof(DeleteMedia), new { id });
        }
    }

}
