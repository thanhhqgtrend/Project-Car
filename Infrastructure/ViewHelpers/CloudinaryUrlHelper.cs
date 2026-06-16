using LuxuryCar.Models;

namespace LuxuryCar.Infrastructure;

public static class CloudinaryUrlHelper
{
    public const string PublicVehicleCard = "w_640,h_480,c_fill,g_auto,f_auto,q_auto";
    public const string PublicAddonCard = "w_640,h_360,c_fill,g_auto,f_auto,q_auto";
    public const string PublicRouteCard = "w_640,h_360,c_fill,g_auto,f_auto,q_auto";
    public const string AddonCheckoutThumb = "w_180,h_135,c_fill,g_auto,f_auto,q_auto";
    public const string AdminVehicleThumb = "w_160,h_120,c_fill,g_auto,f_auto,q_auto";
    public const string VehicleEditPreview = "w_720,h_450,c_fill,g_auto,f_auto,q_auto";
    public const string Detail = "w_1200,c_limit,f_auto,q_auto";
    public const string MediaLibraryCard = "w_320,h_240,c_fill,g_auto,f_auto,q_auto";
    public const string BlogCard = "w_640,h_360,c_fill,g_auto,f_auto,q_auto";
    public const string BlogHero = "w_1280,h_720,c_fill,g_auto,f_auto,q_auto";
    public const string DeletePreview = "w_720,c_limit,f_auto,q_auto";

    public static string For(MediaAsset? asset, string transformation)
    {
        return Transform(asset?.SecureUrl, transformation);
    }

    public static string Transform(string? secureUrl, string transformation)
    {
        if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(transformation))
        {
            return secureUrl ?? string.Empty;
        }

        const string uploadSegment = "/upload/";
        var uploadIndex = secureUrl.IndexOf(uploadSegment, StringComparison.OrdinalIgnoreCase);
        if (uploadIndex < 0)
        {
            return secureUrl;
        }

        var normalizedTransformation = transformation.Trim('/');
        if (string.IsNullOrWhiteSpace(normalizedTransformation))
        {
            return secureUrl;
        }

        var insertIndex = uploadIndex + uploadSegment.Length;
        return secureUrl.Insert(insertIndex, normalizedTransformation + "/");
    }
}
