using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TransportPipes.Database.Helpers;

/// <summary>
/// PostgreSQL "timestamp with time zone" only accepts UTC DateTimes. All game times are UTC, this converter makes sure
/// DateTimes without a Kind (e.g. new DateTime(2015, 1, 1) in tests) are treated as UTC and that values read back are UTC.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
