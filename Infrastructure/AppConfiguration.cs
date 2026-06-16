using System.Configuration;

namespace LuxuryCar.Infrastructure
{
    public interface IAppConfiguration
    {
        string Get(string key, string defaultValue = "");
    }

    public class AppConfiguration : IAppConfiguration
    {
        public string Get(string key, string defaultValue = "")
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
