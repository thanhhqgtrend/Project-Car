using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LuxuryCar.Infrastructure
{

    public class CustomerAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var identity = httpContext?.User?.Identity;
            return identity != null
                && identity.IsAuthenticated
                && string.Equals(identity.AuthenticationType, CustomerAuthentication.CookieAuthenticationType, System.StringComparison.Ordinal);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var returnUrl = filterContext?.HttpContext?.Request?.RawUrl ?? "/account/my-bookings";
            filterContext.Result = new RedirectResult("/account/login?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }
    }
}