using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using LuxuryCar.Infrastructure.Startup;

namespace LuxuryCar
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly IDictionary<string, string> SupportedCultures =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = "en-US",
                ["vi"] = "vi-VN",
                ["ko"] = "ko-KR",
                ["zh"] = "zh-CN",
                ["ja"] = "ja-JP",
                ["fr"] = "fr-FR"
            };

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutofacConfig.Register();
        }

        protected void Application_BeginRequest()
        {
            var cultureCode = Request.Cookies[".AspNet.Culture"]?.Value ?? "en";
            if (!SupportedCultures.TryGetValue(cultureCode, out var cultureName))
            {
                cultureName = SupportedCultures["en"];
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
