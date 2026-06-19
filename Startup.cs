using System;
using LuxuryCar.Infrastructure;
using LuxuryCar.Infrastructure.Startup;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(LuxuryCar.Startup))]

namespace LuxuryCar
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext(Data.ApplicationDbContext.Create);
            app.CreatePerOwinContext<Identity.ApplicationUserManager>(Identity.ApplicationUserManager.Create);
            app.CreatePerOwinContext<Identity.ApplicationSignInManager>(Identity.ApplicationSignInManager.Create);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/admin/login"),
                ExpireTimeSpan = TimeSpan.FromHours(8),
                SlidingExpiration = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<Identity.ApplicationUserManager, Identity.ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CustomerAuthentication.CookieAuthenticationType,
                CookieName = "LuxuryCar.Customer",
                LoginPath = new PathString("/account/login"),
                ExpireTimeSpan = TimeSpan.FromDays(14),
                SlidingExpiration = true,
                AuthenticationMode = AuthenticationMode.Passive
            });

            AutofacConfig.ConfigureOwin(app);
        }
    }
}