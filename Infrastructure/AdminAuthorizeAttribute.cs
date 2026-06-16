using System.Web;
using System.Web.Mvc;

namespace LuxuryCar.Infrastructure
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext?.User?.Identity == null)
            {
                return false;
            }

            return base.AuthorizeCore(httpContext);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var user = filterContext?.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                filterContext.Result = new HttpStatusCodeResult(403);
                return;
            }

            var returnUrl = filterContext?.HttpContext?.Request?.RawUrl ?? "/admin";
            filterContext.Result = new RedirectResult("/admin/login?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }
    }
}
