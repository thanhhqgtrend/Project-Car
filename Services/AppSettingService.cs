using System.Globalization;
using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Models;
using System.Data.Entity;

namespace LuxuryCar.Services;

public class AppSettingService : IAppSettingService
{
    private readonly ApplicationDbContext _db;
    private readonly IAppConfiguration _configuration;

    public AppSettingService(ApplicationDbContext db, IAppConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<string> GetAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default)
    {
        var storedValue = await _db.AppSettings
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(storedValue))
        {
            return storedValue;
        }

        return _configuration.Get(key, defaultValue);
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, string.Empty, cancellationToken).ConfigureAwait(false);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, string.Empty, cancellationToken).ConfigureAwait(false);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    public async Task SetAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken).ConfigureAwait(false);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value?.Trim() ?? string.Empty });
            return;
        }

        setting.Value = value?.Trim() ?? string.Empty;
        setting.UpdatedAtUtc = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
