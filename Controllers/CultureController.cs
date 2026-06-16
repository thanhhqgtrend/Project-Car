using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace LuxuryCar.Controllers;

public class CultureController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "vi",
        "ko",
        "zh",
        "ja",
        "fr"
    };

    [Route("culture/set")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Set(string culture, string? returnUrl)
    {
        var selectedCulture = SupportedCultures.Contains(culture) ? culture.ToLowerInvariant() : "en";
        Response.Cookies.Set(new HttpCookie(".AspNet.Culture", selectedCulture)
        {
            Expires = DateTime.UtcNow.AddYears(1),
            HttpOnly = false,
            Path = "/"
        });

        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
