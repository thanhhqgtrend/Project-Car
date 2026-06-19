using System;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace LuxuryCar.Infrastructure
{
    public class CustomerAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var owinContext = GetOwinContext(httpContext);
            if (owinContext == null)
            {
                return false;
            }

            var result = owinContext.Authentication
                .AuthenticateAsync(CustomerAuthentication.CookieAuthenticationType)
                .GetAwaiter()
                .GetResult();

            return result?.Identity?.IsAuthenticated == true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var returnUrl = filterContext?.HttpContext?.Request?.RawUrl ?? "/account/my-bookings";
            filterContext.Result = new RedirectResult("/account/login?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }

        private static IOwinContext? GetOwinContext(HttpContextBase httpContext)
        {
            var environment = httpContext?.Items["owin.Environment"] as System.Collections.Generic.IDictionary<string, object>;
            return environment == null ? null : new OwinContext(environment);
        }
    }
}