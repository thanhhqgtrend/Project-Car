using LuxuryCar.Data;
using LuxuryCar.Models;
using System.Data.Entity;
using System.Data;

namespace LuxuryCar.Services;

public class BookingNumberService : IBookingNumberService
{
    private const int CounterId = 1;
    private const string Prefix = "C";
    private const int NumberWidth = 5;
    private const int BookingNumberLength = 6;
    private readonly ApplicationDbContext _db;

    public BookingNumberService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);

        var counter = await _db.BookingCounters
            .SingleOrDefaultAsync(x => x.Id == CounterId, cancellationToken);

        if (counter is null)
        {
            counter = new BookingCounter { Id = CounterId, LastNumber = 0 };
            _db.BookingCounters.Add(counter);
        }

        var lastUsedNumber = await GetLastUsedNumberAsync(cancellationToken);
        counter.LastNumber = lastUsedNumber == 0
            ? 0
            : Math.Max(counter.LastNumber, lastUsedNumber);

        string bookingNumber;
        do
        {
            counter.LastNumber++;
            bookingNumber = Format(counter.LastNumber);
        }
        while (await _db.Bookings.AnyAsync(x => x.BookingNumber == bookingNumber, cancellationToken));

        await _db.SaveChangesAsync(cancellationToken);
        transaction.Commit();

        return bookingNumber;
    }

    private static string Format(int number) => $"{Prefix}{number.ToString().PadLeft(NumberWidth, '0')}";

    private async Task<int> GetLastUsedNumberAsync(CancellationToken cancellationToken)
    {
        var bookingNumbers = await _db.Bookings
            .Where(x => x.BookingNumber.StartsWith(Prefix) && x.BookingNumber.Length == BookingNumberLength)
            .Select(x => x.BookingNumber)
            .ToListAsync(cancellationToken);

        return bookingNumbers
            .Select(ParseNumber)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static int ParseNumber(string bookingNumber) =>
        int.TryParse(bookingNumber.Substring(Prefix.Length), out var number) ? number : 0;
}
