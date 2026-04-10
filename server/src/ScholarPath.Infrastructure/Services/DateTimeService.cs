using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public sealed class DateTimeService : IDateTimeService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
