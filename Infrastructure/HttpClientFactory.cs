using System.Net.Http;

namespace LuxuryCar.Infrastructure
{
    public interface IHttpClientFactory
    {
        HttpClient CreateClient();
    }

    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient Client = new HttpClient();

        public HttpClient CreateClient()
        {
            return Client;
        }
    }
}
