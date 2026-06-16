using LuxuryCar.Models;

namespace LuxuryCar.Services;

public record QuoteResult(decimal DistanceKm, DistanceStatus DistanceStatus, decimal PriceUsd);
