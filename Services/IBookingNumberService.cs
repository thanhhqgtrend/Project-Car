namespace LuxuryCar.Services;

public interface IBookingNumberService
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
