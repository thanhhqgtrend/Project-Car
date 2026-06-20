using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using LuxuryCar.Data;
using LuxuryCar.Identity;
using LuxuryCar.Infrastructure;
using LuxuryCar.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using LuxuryCar.Models;
using Microsoft.Owin.Security;

namespace LuxuryCar.Controllers
{
    [RoutePrefix("account")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        private IOwinContext OwinContext
        {
            get
            {
                var environment = (System.Collections.Generic.IDictionary<string, object>)
                    System.Web.HttpContext.Current.Items["owin.Environment"];
                return new OwinContext(environment);
            }
        }

        private ApplicationUserManager UserManager => OwinContext.GetUserManager<ApplicationUserManager>();

        private IAuthenticationManager AuthenticationManager => OwinContext.Authentication;

        [Route("register")]
        [HttpGet]
        public async Task<ActionResult> Register(string? returnUrl)
        {
            var identity = await GetCustomerIdentityAsync();
            if (IsCustomerSignedIn(identity))
            {
                return RedirectToLocal(returnUrl, "/");
            }

            return View(new CustomerRegisterViewModel { ReturnUrl = returnUrl });
        }

        [Route("register")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(CustomerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await UserManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View(model);
            }

            await UserManager.AddToRoleAsync(user.Id, "Customer");
            await SignInCustomerAsync(user, isPersistent: false);

            return RedirectToLocal(model.ReturnUrl, "/");
        }

        [Route("login")]
        [HttpGet]
        public async Task<ActionResult> Login(string? returnUrl)
        {
            var identity = await GetCustomerIdentityAsync();
            if (IsCustomerSignedIn(identity))
            {
                return RedirectToLocal(returnUrl, "/");
            }

            return View(new CustomerLoginViewModel { ReturnUrl = returnUrl });
        }

        [Route("login")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(CustomerLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null || !await UserManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            await SignInCustomerAsync(user, model.RememberMe);

            return RedirectToLocal(model.ReturnUrl, "/");
        }

        [Route("logout")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(CustomerAuthentication.CookieAuthenticationType);
            return Redirect("/");
        }

        [Route("my-bookings")]
        [HttpGet]
        [CustomerAuthorize]
        public async Task<ActionResult> MyBookings()
        {
            var identity = await GetCustomerIdentityAsync();
            var userId = CustomerIdFromIdentity(identity);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var bookings = await _db.Bookings
                .AsNoTracking()
                .Include(x => x.CarVehicleType)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();

            var bookingIds = bookings.Select(x => x.Id).ToList();
            ViewBag.ReviewedBookingIds = await _db.BookingReviews
                .Where(x => bookingIds.Contains(x.BookingId))
                .Select(x => x.BookingId)
                .ToListAsync();

            return View(bookings);
        }

        [Route("my-bookings/{bookingId:int}/review")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomerAuthorize]
        public async Task<ActionResult> SubmitReview(int bookingId, int rating, string? comment)
        {
            var identity = await GetCustomerIdentityAsync();
            var userId = CustomerIdFromIdentity(identity);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Please choose a rating between 1 and 5 stars.";
                return RedirectToAction("MyBookings");
            }

            var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);
            if (booking == null)
            {
                return HttpNotFound();
            }

            var isCompleted = booking.PickupDateTimeUtc < DateTime.UtcNow
                && (booking.Status == BookingStatus.Paid || booking.Status == BookingStatus.Confirmed);
            if (!isCompleted)
            {
                TempData["ReviewError"] = "This trip is not eligible for a review yet.";
                return RedirectToAction("MyBookings");
            }

            var alreadyReviewed = await _db.BookingReviews.AnyAsync(x => x.BookingId == bookingId);
            if (alreadyReviewed)
            {
                TempData["ReviewError"] = "You've already reviewed this booking.";
                return RedirectToAction("MyBookings");
            }

            _db.BookingReviews.Add(new BookingReview
            {
                BookingId = bookingId,
                UserId = userId,
                Rating = rating,
                Comment = comment?.Trim() ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Thank you for your review!";
            return RedirectToAction("MyBookings");
        }

        [Route("profile")]
        [HttpGet]
        [CustomerAuthorize]
        public async Task<ActionResult> Profile()
        {
            var identity = await GetCustomerIdentityAsync();
            var userId = CustomerIdFromIdentity(identity);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(new ProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneCountryCode = string.IsNullOrWhiteSpace(user.PhoneCountryCode) ? "+84" : user.PhoneCountryCode,
                PhoneNumber = user.PhoneNumber
            });
        }

        [Route("profile")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomerAuthorize]
        public async Task<ActionResult> Profile(ProfileViewModel model)
        {
            var identity = await GetCustomerIdentityAsync();
            var userId = CustomerIdFromIdentity(identity);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                var current = await UserManager.FindByIdAsync(userId);
                model.Email = current?.Email ?? model.Email ?? string.Empty;
                return View(model);
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? null : model.FirstName.Trim();
            user.LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName.Trim();
            user.PhoneCountryCode = string.IsNullOrWhiteSpace(model.PhoneCountryCode) ? null : model.PhoneCountryCode.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                model.Email = user.Email ?? string.Empty;
                return View(model);
            }

            await SignInCustomerAsync(user, isPersistent: true);

            TempData["ProfileSuccess"] = "Your profile has been updated.";
            return RedirectToAction("Profile");
        }

        [Route("profile/change-password")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomerAuthorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var identity = await GetCustomerIdentityAsync();
            var userId = CustomerIdFromIdentity(identity);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                TempData["PasswordError"] = firstError ?? "Please check your password entries.";
                return RedirectToAction("Profile");
            }

            var result = await UserManager.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                TempData["PasswordError"] = string.Join(" ", result.Errors);
                return RedirectToAction("Profile");
            }

            TempData["PasswordSuccess"] = "Your password has been changed.";
            return RedirectToAction("Profile");
        }

        private async Task SignInCustomerAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(CustomerAuthentication.CookieAuthenticationType);

            var roles = await UserManager.GetRolesAsync(user.Id);

            var identity = new ClaimsIdentity(CustomerAuthentication.CookieAuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : (user.UserName ?? user.Email ?? string.Empty)));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = isPersistent }, identity);
        }

        private async Task<ClaimsIdentity?> GetCustomerIdentityAsync()
        {
            var result = await AuthenticationManager.AuthenticateAsync(CustomerAuthentication.CookieAuthenticationType);
            return result?.Identity;
        }

        private bool IsCustomerSignedIn(ClaimsIdentity? identity) =>
            identity?.IsAuthenticated == true;

        private static string? CustomerIdFromIdentity(ClaimsIdentity? identity) =>
            identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private ActionResult RedirectToLocal(string? returnUrl, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect(fallback);
        }
    }
}
