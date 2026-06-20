using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LuxuryCar.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? PhoneCountryCode { get; set; }

        public string? DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? null
            : $"{FirstName} {LastName}".Trim();

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }
}