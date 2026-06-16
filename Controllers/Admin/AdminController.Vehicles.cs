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
    [Route("vehicles")]
    [HttpGet]
    public async Task<ActionResult> Vehicles(string? status = "active", string? search = null)
    {
        status = string.IsNullOrWhiteSpace(status) ? "active" : status.ToLowerInvariant();
        var query = _db.CarVehicleTypes.Include(x => x.Image).AsNoTracking().AsQueryable();

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

        var model = new AdminCarVehicleListViewModel
        {
            Status = status,
            Search = search ?? string.Empty,
            Vehicles = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync()
        };

        return View("CarVehicles", model);
    }

    [Route("vehicles/create")]
    [HttpGet]
    public async Task<ActionResult> CreateVehicle()
    {
        ViewData["VehicleFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = new CarVehicleTypeFormViewModel();
        await PopulateVehicleMediaAsync(model);
        return View("CarVehicleForm", model);
    }

    [Route("vehicles/create")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateVehicle(CarVehicleTypeFormViewModel model)
    {
        ViewData["VehicleFormMode"] = "Create";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (!ModelState.IsValid)
        {
            await PopulateVehicleMediaAsync(model);
            return View("CarVehicleForm", model);
        }

        var vehicle = new CarVehicleType();
        if (!await TryApplyUploadedVehicleImageAsync(model))
        {
            await PopulateVehicleMediaAsync(model);
            return View("CarVehicleForm", model);
        }
        model.ApplyTo(vehicle);
        _db.CarVehicleTypes.Add(vehicle);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Vehicle created.";
        return RedirectToAction(nameof(VehicleDetails), new { id = vehicle.Id });
    }

    [Route("vehicles/{id:int}")]
    [HttpGet]
    public async Task<ActionResult> VehicleDetails(int id)
    {
        var vehicle = await _db.CarVehicleTypes.Include(x => x.Image).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return vehicle is null ? HttpNotFound() : View("CarVehicleDetails", vehicle);
    }

    [Route("vehicles/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditVehicle(int id)
    {
        var vehicle = await _db.CarVehicleTypes.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
        if (vehicle is null)
        {
            return HttpNotFound();
        }

        ViewData["VehicleFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        var model = CarVehicleTypeFormViewModel.FromEntity(vehicle);
        await PopulateVehicleMediaAsync(model);
        return View("CarVehicleForm", model);
    }

    [Route("vehicles/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditVehicle(int id, CarVehicleTypeFormViewModel model)
    {
        ViewData["VehicleFormMode"] = "Edit";
        ViewData["CloudinaryConfigured"] = await _mediaStorage.IsConfiguredAsync();
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var vehicle = await _db.CarVehicleTypes.FindAsync(id);
        if (vehicle is null)
        {
            return HttpNotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateVehicleMediaAsync(model);
            return View("CarVehicleForm", model);
        }

        if (!await TryApplyUploadedVehicleImageAsync(model))
        {
            await PopulateVehicleMediaAsync(model);
            return View("CarVehicleForm", model);
        }
        model.ApplyTo(vehicle);
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Vehicle updated.";
        return RedirectToAction(nameof(VehicleDetails), new { id = vehicle.Id });
    }

    [Route("vehicles/{id:int}/delete")]
    [HttpGet]
    public async Task<ActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _db.CarVehicleTypes.Include(x => x.Image).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return vehicle is null ? HttpNotFound() : View("DeleteCarVehicle", vehicle);
    }

    [Route("vehicles/{id:int}/delete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteVehicleConfirmed(int id)
    {
        var vehicle = await _db.CarVehicleTypes.FindAsync(id);
        if (vehicle is null)
        {
            return HttpNotFound();
        }

        if (!vehicle.IsActive)
        {
            TempData["AdminNotice"] = "Vehicle already inactive.";
            return RedirectToAction(nameof(Vehicles), new { status = "inactive" });
        }

        vehicle.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Vehicle deactivated.";
        return RedirectToAction(nameof(Vehicles), new { status = "inactive" });
    }

}
