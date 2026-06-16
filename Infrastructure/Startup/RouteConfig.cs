using System.Web.Mvc;
using System.Web.Routing;

namespace LuxuryCar.Infrastructure.Startup
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Admin",
                url: "admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Bookings", id = UrlParameter.Optional });

            routes.MapRoute(
                name: "BookingResults",
                url: "booking/results",
                defaults: new { controller = "Booking", action = "Results" });

            routes.MapRoute(
                name: "BookingCheckout",
                url: "booking/checkout",
                defaults: new { controller = "Booking", action = "Checkout" });

            routes.MapRoute(
                name: "CmsPublicPath",
                url: "{*path}",
                defaults: new { controller = "Home", action = "CmsPath" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }
    }
}
