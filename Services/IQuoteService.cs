using LuxuryCar.Models;

namespace LuxuryCar.Services;

public interface IQuoteService
{
    Task<QuoteResult> QuoteAsync(Airport airport, CarVehicleType vehicle, string address, CancellationToken cancellationToken = default);
    Task<QuoteResult> QuoteRouteAsync(string origin, string destination, CarVehicleType vehicle, CancellationToken cancellationToken = default);
    QuoteResult QuoteHire(CarVehicleType vehicle);
    Task<QuoteDistanceResult> GetAirportDistanceAsync(Airport airport, string address, CancellationToken cancellationToken = default);
    Task<QuoteDistanceResult> GetRouteDistanceAsync(string origin, string destination, CancellationToken cancellationToken = default);
    QuoteResult QuoteFromDistance(CarVehicleType vehicle, QuoteDistanceResult distance);
}
