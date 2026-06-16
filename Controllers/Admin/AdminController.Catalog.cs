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
    [Route("airports")]
    [HttpGet]
    public async Task<ActionResult> Airports(string? status = "active", string? search = null)
    {
        status = string.IsNullOrWhiteSpace(status) ? "active" : status.ToLowerInvariant();
        var query = _db.Airports.AsNoTracking().AsQueryable();
        query = status switch
        {
            "inactive" => query.Where(x => !x.IsActive),
            "all" => query,
            _ => query.Where(x => x.IsActive)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search) || x.City.Contains(search));
        }

        ViewData["Status"] = status;
        ViewData["Search"] = search ?? string.Empty;
        return View(await query.OrderBy(x => x.City).ThenBy(x => x.Code).ToListAsync());
    }

    [Route("airports/create")]
    [HttpGet]
    public ActionResult CreateAirport()
    {
        ViewData["AirportFormMode"] = "Create";
        return View("AirportForm", new AirportFormViewModel());
    }

    [Route("airports/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateAirport(AirportFormViewModel model)
    {
        ViewData["AirportFormMode"] = "Create";
        await ValidateAirportCodeAsync(model);
        if (!ModelState.IsValid)
        {
            return View("AirportForm", model);
        }

        var airport = new Airport();
        model.ApplyTo(airport);
        _db.Airports.Add(airport);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Airport created.";
        return RedirectToAction(nameof(Airports));
    }

    [Route("airports/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditAirport(int id)
    {
        var airport = await _db.Airports.FindAsync(id);
        if (airport is null)
        {
            return HttpNotFound();
        }

        ViewData["AirportFormMode"] = "Edit";
        return View("AirportForm", AirportFormViewModel.FromEntity(airport));
    }

    [Route("airports/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditAirport(int id, AirportFormViewModel model)
    {
        ViewData["AirportFormMode"] = "Edit";
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var airport = await _db.Airports.FindAsync(id);
        if (airport is null)
        {
            return HttpNotFound();
        }

        await ValidateAirportCodeAsync(model);
        if (!ModelState.IsValid)
        {
            return View("AirportForm", model);
        }

        model.ApplyTo(airport);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Airport saved.";
        return RedirectToAction(nameof(Airports));
    }

    [Route("airports/{id:int}/deactivate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeactivateAirport(int id)
    {
        var airport = await _db.Airports.FindAsync(id);
        if (airport is null)
        {
            return HttpNotFound();
        }

        airport.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Airport deactivated.";
        return RedirectToAction(nameof(Airports), new { status = "inactive" });
    }

    [Route("addons")]
    [HttpGet]
    public async Task<ActionResult> Addons(string? status = "active", string? search = null)
    {
        status = string.IsNullOrWhiteSpace(status) ? "active" : status.ToLowerInvariant();
        var query = _db.CarBookingAddons.Include(x => x.Image).AsNoTracking().AsQueryable();
        query = status switch
        {
            "inactive" => query.Where(x => !x.IsActive),
            "all" => query,
            _ => query.Where(x => x.IsActive)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || x.Description.Contains(search));
        }

        ViewData["Status"] = status;
        ViewData["Search"] = search ?? string.Empty;
        return View(await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync());
    }

    [Route("addons/create")]
    [HttpGet]
    public async Task<ActionResult> CreateAddon()
    {
        ViewData["AddonFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = new CarBookingAddonFormViewModel();
        await PopulateAddonMediaAsync(model);
        return View("AddonForm", model);
    }

    [Route("addons/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateAddon(CarBookingAddonFormViewModel model)
    {
        ViewData["AddonFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (!ModelState.IsValid)
        {
            await PopulateAddonMediaAsync(model);
            return View("AddonForm", model);
        }

        var addon = new CarBookingAddon();
        if (!await TryApplyUploadedAddonImageAsync(model))
        {
            await PopulateAddonMediaAsync(model);
            return View("AddonForm", model);
        }
        model.ApplyTo(addon);
        _db.CarBookingAddons.Add(addon);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Addon created.";
        return RedirectToAction(nameof(Addons));
    }

    [Route("addons/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditAddon(int id)
    {
        var addon = await _db.CarBookingAddons.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
        if (addon is null)
        {
            return HttpNotFound();
        }

        ViewData["AddonFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = CarBookingAddonFormViewModel.FromEntity(addon);
        await PopulateAddonMediaAsync(model);
        return View("AddonForm", model);
    }

    [Route("addons/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditAddon(int id, CarBookingAddonFormViewModel model)
    {
        ViewData["AddonFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var addon = await _db.CarBookingAddons.FindAsync(id);
        if (addon is null)
        {
            return HttpNotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateAddonMediaAsync(model);
            return View("AddonForm", model);
        }

        if (!await TryApplyUploadedAddonImageAsync(model))
        {
            await PopulateAddonMediaAsync(model);
            return View("AddonForm", model);
        }

        model.ApplyTo(addon);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Addon saved.";
        return RedirectToAction(nameof(Addons));
    }

    [Route("addons/{id:int}/deactivate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeactivateAddon(int id)
    {
        var addon = await _db.CarBookingAddons.FindAsync(id);
        if (addon is null)
        {
            return HttpNotFound();
        }

        addon.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Addon deactivated.";
        return RedirectToAction(nameof(Addons), new { status = "inactive" });
    }

    [Route("popular-routes")]
    [HttpGet]
    public async Task<ActionResult> PopularRoutes(string? status = "active", string? search = null)
    {
        status = string.IsNullOrWhiteSpace(status) ? "active" : status.ToLowerInvariant();
        var query = _db.PopularRoutes.Include(x => x.Image).AsNoTracking().AsQueryable();
        query = status switch
        {
            "inactive" => query.Where(x => !x.IsActive),
            "all" => query,
            _ => query.Where(x => x.IsActive)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.PriceLabel.Contains(search) || x.Description.Contains(search));
        }

        ViewData["Status"] = status;
        ViewData["Search"] = search ?? string.Empty;
        return View(await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Title).ToListAsync());
    }

    [Route("popular-routes/create")]
    [HttpGet]
    public async Task<ActionResult> CreatePopularRoute()
    {
        ViewData["PopularRouteFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = new PopularRouteFormViewModel();
        await PopulatePopularRouteMediaAsync(model);
        return View("PopularRouteForm", model);
    }

    [Route("popular-routes/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePopularRoute(PopularRouteFormViewModel model)
    {
        ViewData["PopularRouteFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (!ModelState.IsValid)
        {
            await PopulatePopularRouteMediaAsync(model);
            return View("PopularRouteForm", model);
        }

        if (!await TryApplyUploadedPopularRouteImageAsync(model))
        {
            await PopulatePopularRouteMediaAsync(model);
            return View("PopularRouteForm", model);
        }

        var route = new PopularRoute();
        model.ApplyTo(route);
        _db.PopularRoutes.Add(route);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Popular route created.";
        return RedirectToAction(nameof(PopularRoutes));
    }

    [Route("popular-routes/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditPopularRoute(int id)
    {
        var route = await _db.PopularRoutes.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
        if (route is null)
        {
            return HttpNotFound();
        }

        ViewData["PopularRouteFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = PopularRouteFormViewModel.FromEntity(route);
        await PopulatePopularRouteMediaAsync(model);
        return View("PopularRouteForm", model);
    }

    [Route("popular-routes/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditPopularRoute(int id, PopularRouteFormViewModel model)
    {
        ViewData["PopularRouteFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var route = await _db.PopularRoutes.FindAsync(id);
        if (route is null)
        {
            return HttpNotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulatePopularRouteMediaAsync(model);
            return View("PopularRouteForm", model);
        }

        if (!await TryApplyUploadedPopularRouteImageAsync(model))
        {
            await PopulatePopularRouteMediaAsync(model);
            return View("PopularRouteForm", model);
        }

        model.ApplyTo(route);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Popular route saved.";
        return RedirectToAction(nameof(PopularRoutes));
    }

    [Route("popular-routes/{id:int}/deactivate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeactivatePopularRoute(int id)
    {
        var route = await _db.PopularRoutes.FindAsync(id);
        if (route is null)
        {
            return HttpNotFound();
        }

        route.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Popular route deactivated.";
        return RedirectToAction(nameof(PopularRoutes), new { status = "inactive" });
    }

}
