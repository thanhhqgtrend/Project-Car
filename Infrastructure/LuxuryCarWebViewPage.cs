using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Web.Mvc;
using LuxuryCar.Services;

namespace LuxuryCar.Infrastructure
{
    public abstract class LuxuryCarWebViewPage : WebViewPage
    {
        public IAppSettingService AppSettings => DependencyResolver.Current.GetService<IAppSettingService>();
        public ViewTextLocalizer L { get; } = new ViewTextLocalizer();
    }

    public abstract class LuxuryCarWebViewPage<TModel> : WebViewPage<TModel>
    {
        public IAppSettingService AppSettings => DependencyResolver.Current.GetService<IAppSettingService>();
        public ViewTextLocalizer L { get; } = new ViewTextLocalizer();
    }

    public class ViewTextLocalizer
    {
        private static readonly ResourceManager Resources =
            new ResourceManager("LuxuryCar.Resources.SharedResource", Assembly.GetExecutingAssembly());

        public ViewText this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return new ViewText(string.Empty);
                }

                try
                {
                    var value = Resources.GetString(key, CultureInfo.CurrentUICulture);
                    return new ViewText(string.IsNullOrWhiteSpace(value) ? key : value);
                }
                catch (MissingManifestResourceException)
                {
                    return new ViewText(key);
                }
                catch (MissingSatelliteAssemblyException)
                {
                    return new ViewText(key);
                }
            }
        }
    }

    public sealed class ViewText
    {
        public ViewText(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString() => Value;

        public static implicit operator string(ViewText text) => text?.Value ?? string.Empty;
    }
}
