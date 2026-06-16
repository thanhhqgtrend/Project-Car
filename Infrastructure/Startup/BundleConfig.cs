using System.Web.Optimization;

namespace LuxuryCar.Infrastructure.Startup
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            var siteScripts = new ScriptBundle("~/bundles/site").Include(
                "~/wwwroot/js/site/00-clock.js",
                "~/wwwroot/js/site/10-geoapify-autocomplete.js",
                "~/wwwroot/js/site/20-booking-search.js",
                "~/wwwroot/js/site/30-checkout-summary.js",
                "~/wwwroot/js/site/40-admin-ui.js");
            siteScripts.Transforms.Clear();
            bundles.Add(siteScripts);

            var siteStyles = new StyleBundle("~/Content/site").Include(
                "~/wwwroot/css/site/00-base-layout.css",
                "~/wwwroot/css/site/10-public-content.css",
                "~/wwwroot/css/site/20-booking-payment-core.css",
                "~/wwwroot/css/site/30-admin.css",
                "~/wwwroot/css/site/40-booking-checkout.css",
                "~/wwwroot/css/site/90-responsive-utilities.css");
            siteStyles.Transforms.Clear();
            bundles.Add(siteStyles);

            BundleTable.EnableOptimizations = false;
        }
    }
}
