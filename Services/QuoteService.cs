using System.Text.Json;
using System.Text.Json.Serialization;
using LuxuryCar.Infrastructure;
using LuxuryCar.Models;
using System.Data.Entity;

namespace LuxuryCar.Services;

public class QuoteService : IQuoteService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppSettingService _settings;
    private readonly IAppLogger<QuoteService> _logger;
    private readonly IRuntimeCache _cache;
    private const double MinimumGeocodeConfidence = 0.4;
    private const decimal VietnamBiasLatitude = 16.0m;
    private const decimal VietnamBiasLongitude = 107.8m;

    public QuoteService(IHttpClientFactory httpClientFactory, IAppSettingService settings, IAppLogger<QuoteService> logger, IRuntimeCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
        _cache = cache;
    }

    public async Task<QuoteResult> QuoteAsync(Airport airport, CarVehicleType vehicle, string address, CancellationToken cancellationToken = default)
    {
        var distance = await GetAirportDistanceAsync(airport, address, cancellationToken);
        return QuoteFromDistance(vehicle, distance);
    }

    public async Task<QuoteResult> QuoteRouteAsync(string origin, string destination, CarVehicleType vehicle, CancellationToken cancellationToken = default)
    {
        var distance = await GetRouteDistanceAsync(origin, destination, cancellationToken);
        return QuoteFromDistance(vehicle, distance);
    }

    public QuoteResult QuoteHire(CarVehicleType vehicle)
    {
        const decimal defaultHireDistanceKm = 25m;
        var price = Math.Round(vehicle.BaseFareUsd + defaultHireDistanceKm * vehicle.PricePerKmUsd, 2);
        return new QuoteResult(defaultHireDistanceKm, DistanceStatus.Estimated, price);
    }

    public async Task<QuoteDistanceResult> GetAirportDistanceAsync(Airport airport, string address, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetGeoapifyApiKeyAsync(cancellationToken);
        var distance = EstimateFallbackDistance(airport, address);
        var status = DistanceStatus.Estimated;

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(address))
        {
            try
            {
                var meters = await GetRouteDistanceMetersAsync(
                    new GeoapifyCoordinate(airport.Latitude, airport.Longitude),
                    address,
                    apiKey,
                    cancellationToken);

                if (meters is > 0)
                {
                    distance = Math.Round(meters.Value / 1000m, 2);
                    status = DistanceStatus.Geoapify;
                }
                else
                {
                    status = DistanceStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geoapify distance lookup failed; using fallback estimate.");
                status = DistanceStatus.Failed;
            }
        }

        return new QuoteDistanceResult(distance, status);
    }

    public async Task<QuoteDistanceResult> GetRouteDistanceAsync(string origin, string destination, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetGeoapifyApiKeyAsync(cancellationToken);
        var distance = EstimateFallbackDistance(origin, destination);
        var status = DistanceStatus.Estimated;

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(origin) && !string.IsNullOrWhiteSpace(destination))
        {
            try
            {
                var meters = await GetRouteDistanceMetersAsync(origin, destination, apiKey, cancellationToken);
                if (meters is > 0)
                {
                    distance = Math.Round(meters.Value / 1000m, 2);
                    status = DistanceStatus.Geoapify;
                }
                else
                {
                    status = DistanceStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geoapify route distance lookup failed; using fallback estimate.");
                status = DistanceStatus.Failed;
            }
        }

        return new QuoteDistanceResult(distance, status);
    }

    public QuoteResult QuoteFromDistance(CarVehicleType vehicle, QuoteDistanceResult distance)
    {
        var price = Math.Round(vehicle.BaseFareUsd + distance.DistanceKm * vehicle.PricePerKmUsd, 2);
        return new QuoteResult(distance.DistanceKm, distance.DistanceStatus, price);
    }

    private static decimal EstimateFallbackDistance(Airport airport, string address)
    {
        var seed = Math.Abs(string.Concat(airport.Code, "|", address?.Trim().ToLowerInvariant()).GetHashCode());
        return 8 + seed % 42;
    }

    private static decimal EstimateFallbackDistance(string origin, string destination)
    {
        var seed = Math.Abs(string.Concat(origin?.Trim().ToLowerInvariant(), "|", destination?.Trim().ToLowerInvariant()).GetHashCode());
        return 20 + seed % 130;
    }

    private async Task<string> GetGeoapifyApiKeyAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "settings:Geoapify:ApiKey";
        if (_cache.TryGetValue<string>(cacheKey, out var cachedApiKey))
        {
            return cachedApiKey ?? string.Empty;
        }

        var apiKey = await _settings.GetAsync("Geoapify:ApiKey", cancellationToken: cancellationToken);
        _cache.Set(cacheKey, apiKey, TimeSpan.FromMinutes(5));
        return apiKey;
    }

    private async Task<int?> GetRouteDistanceMetersAsync(GeoapifyCoordinate origin, string destinationAddress, string apiKey, CancellationToken cancellationToken)
    {
        var destination = await GeocodeAsync(destinationAddress, apiKey, cancellationToken);
        return destination is null
            ? null
            : await GetRouteDistanceMetersAsync(origin, destination, apiKey, cancellationToken);
    }

    private async Task<int?> GetRouteDistanceMetersAsync(string originAddress, string destinationAddress, string apiKey, CancellationToken cancellationToken)
    {
        var origin = await GeocodeAsync(originAddress, apiKey, cancellationToken);
        var destination = await GeocodeAsync(destinationAddress, apiKey, cancellationToken);

        return origin is null || destination is null
            ? null
            : await GetRouteDistanceMetersAsync(origin, destination, apiKey, cancellationToken);
    }

    private async Task<int?> GetRouteDistanceMetersAsync(GeoapifyCoordinate origin, GeoapifyCoordinate destination, string apiKey, CancellationToken cancellationToken)
    {
        var routeCacheKey = $"geoapify:route:{origin.Latitude:0.######},{origin.Longitude:0.######}:{destination.Latitude:0.######},{destination.Longitude:0.######}";
        if (_cache.TryGetValue(routeCacheKey, out int cachedDistance))
        {
            return cachedDistance;
        }

        var client = _httpClientFactory.CreateClient();
        var waypoints = $"{origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{destination.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{destination.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var url = $"https://api.geoapify.com/v1/routing?waypoints={Uri.EscapeDataString(waypoints)}&mode=drive&format=json&apiKey={Uri.EscapeDataString(apiKey)}";
        var response = JsonSerializer.Deserialize<GeoapifyRouteResponse>(await client.GetStringAsync(url));
        var distance = response?.Results.FirstOrDefault()?.Distance;
        if (distance is > 0)
        {
            _cache.Set(routeCacheKey, distance.Value, TimeSpan.FromHours(1));
        }

        return distance;
    }

    private async Task<GeoapifyCoordinate?> GeocodeAsync(string address, string apiKey, CancellationToken cancellationToken)
    {
        var normalizedAddress = address.Trim().ToLowerInvariant();
        var geocodeCacheKey = $"geoapify:geocode:en:{normalizedAddress}";
        if (_cache.TryGetValue<GeoapifyCoordinate>(geocodeCacheKey, out var cachedCoordinate))
        {
            return cachedCoordinate;
        }

        var client = _httpClientFactory.CreateClient();
        var bias = $"proximity:{VietnamBiasLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{VietnamBiasLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var url = $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(address)}&filter=countrycode:vn&bias={Uri.EscapeDataString(bias)}&format=json&limit=1&lang=en&apiKey={Uri.EscapeDataString(apiKey)}";
        var response = JsonSerializer.Deserialize<GeoapifyGeocodeResponse>(await client.GetStringAsync(url));
        var result = response?.Results.FirstOrDefault();
        if (result is null)
        {
            return null;
        }

        if (result.Confidence < MinimumGeocodeConfidence)
        {
            _logger.LogWarning(new InvalidOperationException($"Low confidence geocode for '{address}'"),
                $"Geoapify geocode for '{address}' had low confidence ({result.Confidence:0.00}); treating as unresolved.");
            return null;
        }

        var coordinate = new GeoapifyCoordinate(result.Latitude, result.Longitude);
        _cache.Set(geocodeCacheKey, coordinate, TimeSpan.FromHours(12));
        return coordinate;
    }

    private sealed record GeoapifyCoordinate(decimal Latitude, decimal Longitude);

    private sealed class GeoapifyGeocodeResponse
    {
        [JsonPropertyName("results")]
        public List<GeoapifyGeocodeResult> Results { get; set; } = [];
    }

    private sealed class GeoapifyGeocodeResult
    {
        [JsonPropertyName("lat")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("lon")]
        public decimal Longitude { get; set; }

        [JsonPropertyName("rank")]
        public GeoapifyRank? Rank { get; set; }

        [JsonIgnore]
        public double Confidence => Rank?.Confidence ?? 0;
    }

    private sealed class GeoapifyRank
    {
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    private sealed class GeoapifyRouteResponse
    {
        [JsonPropertyName("results")]
        public List<GeoapifyRouteResult> Results { get; set; } = [];
    }

    private sealed class GeoapifyRouteResult
    {
        [JsonPropertyName("distance")]
        public int Distance { get; set; }
    }
}