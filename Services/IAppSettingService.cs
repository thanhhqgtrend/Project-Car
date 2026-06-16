namespace LuxuryCar.Services;

public interface IAppSettingService
{
    Task<string> GetAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue, CancellationToken cancellationToken = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string? value, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
