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
    [Route("bookings")]
    [HttpGet]
    public async Task<ActionResult> Bookings(BookingStatus? status, string? search)
    {
        var query = _db.Bookings.Include(x => x.Airport).Include(x => x.CarVehicleType).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.BookingNumber.Contains(search) || x.FullName.Contains(search) || x.Email.Contains(search));
        }

        var model = new AdminBookingListViewModel
        {
            Status = status,
            Search = search ?? string.Empty,
            Bookings = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync()
        };
        return View(model);
    }

    [Route("bookings/{id:int}")]
    [HttpGet]
    public async Task<ActionResult> BookingDetails(int id)
    {
        var booking = await _db.Bookings
            .Include(x => x.Airport)
            .Include(x => x.CarVehicleType)
            .Include(x => x.CarAddonSelections)
            .FirstOrDefaultAsync(x => x.Id == id);
        return booking is null ? HttpNotFound() : View(booking);
    }

    [Route("bookings/{id:int}/email")]
    [HttpPost]
    [ValidateInput(false)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SendBookingEmail(int id, AdminManualEmailViewModel model)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null)
        {
            return HttpNotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["AdminNotice"] = "Email subject and message are required.";
            return RedirectToAction(nameof(BookingDetails), new { id });
        }

        await _emailService.SendManualBookingEmailAsync(booking, model.Subject.Trim(), model.BodyHtml.Trim());
        TempData["AdminNotice"] = $"Manual email queued for {booking.Email}.";
        return RedirectToAction(nameof(BookingDetails), new { id });
    }

    [Route("bookings/{id:int}/edit")]
    [HttpGet]
    public async Task<ActionResult> EditBooking(int id)
    {
        var booking = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (booking is null)
        {
            return HttpNotFound();
        }

        var model = AdminBookingEditViewModel.FromBooking(booking, ToVietnamTime(booking.PickupDateTimeUtc));
        await PopulateBookingEditOptionsAsync(model);
        return View("BookingEdit", model);
    }

    [Route("bookings/{id:int}/edit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditBooking(int id, AdminBookingEditViewModel model)
    {
        if (id != model.Id)
        {
            return new HttpStatusCodeResult(400);
        }

        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null)
        {
            return HttpNotFound();
        }

        if (!await _db.CarVehicleTypes.AnyAsync(x => x.Id == model.CarVehicleTypeId))
        {
            ModelState.AddModelError(nameof(model.CarVehicleTypeId), "Please select a valid vehicle.");
        }

        if (model.AirportId.HasValue && !await _db.Airports.AnyAsync(x => x.Id == model.AirportId.Value))
        {
            ModelState.AddModelError(nameof(model.AirportId), "Please select a valid airport.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateBookingEditOptionsAsync(model);
            return View("BookingEdit", model);
        }

        booking.TripType = model.TripType;
        booking.Status = model.Status;
        booking.AirportId = model.AirportId;
        booking.CarVehicleTypeId = model.CarVehicleTypeId;
        booking.PickupAddress = model.PickupAddress.Trim();
        booking.DropoffAddress = model.DropoffAddress.Trim();
        booking.PickupDateTimeUtc = ToUtcFromVietnamTime(model.PickupDateTime);
        booking.PassengerCount = model.PassengerCount;
        booking.DistanceKm = model.DistanceKm;
        booking.DistanceStatus = model.DistanceStatus;
        booking.Title = model.Title?.Trim() ?? string.Empty;
        booking.FullName = model.FullName.Trim();
        booking.Email = model.Email.Trim();
        booking.Phone = model.Phone.Trim();
        booking.FlightNumber = model.FlightNumber?.Trim() ?? string.Empty;
        booking.MessagingApp = model.MessagingApp?.Trim() ?? string.Empty;
        booking.MessagingHandle = model.MessagingHandle?.Trim() ?? string.Empty;
        booking.Notes = model.Notes?.Trim() ?? string.Empty;
        booking.CouponCode = model.CouponCode?.Trim() ?? string.Empty;
        booking.BasePriceUsd = model.BasePriceUsd;
        booking.AddonTotalUsd = model.AddonTotalUsd;
        booking.DiscountUsd = model.DiscountUsd;
        booking.TaxFeeUsd = model.TaxFeeUsd;
        booking.TotalPriceUsd = model.TotalPriceUsd;
        booking.EstimatedPriceUsd = model.TotalPriceUsd;
        if (booking.Status == BookingStatus.Paid)
        {
            booking.PaidAtUtc ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["AdminNotice"] = "Booking updated.";
        return RedirectToAction(nameof(BookingDetails), new { id });
    }

    [Route("bookings/{id:int}/status")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateBookingStatus(int id, BookingStatus status)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null)
        {
            return HttpNotFound();
        }

        booking.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(BookingDetails), new { id });
    }

}
