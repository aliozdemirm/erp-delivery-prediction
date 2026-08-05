namespace App.Domain.Prediction;

public sealed class WorkingCalendar
{
    private readonly long _dailyWorkingMinutes;

    public WorkingCalendar(long dailyWorkingMinutes)
    {
        if (dailyWorkingMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(dailyWorkingMinutes));
        
        _dailyWorkingMinutes = dailyWorkingMinutes;
    }

    public bool IsWorkingDay(DateOnly date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }

    public DateTimeOffset AddWorkingMinutes(DateTimeOffset start, long minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        if (minutes == 0) return start;
        
        long remaining = minutes;
        DateTimeOffset current = start;

        // Başlangıç hafta sonuna denk geliyorsa, ilk iş gününe ilerlet:
        while (!IsWorkingDay(DateOnly.FromDateTime(current.DateTime)))
        {
            current = current.AddDays(1);
        }

        // Günleri tek tek atla (bölme işlemi kullanılmaz)
        while (remaining >= _dailyWorkingMinutes)
        {
            current = current.AddDays(1);
            while (!IsWorkingDay(DateOnly.FromDateTime(current.DateTime)))
            {
                current = current.AddDays(1);
            }
            remaining -= _dailyWorkingMinutes;
        }

        // Kalan küsurat dakikayı ekle
        if (remaining > 0)
        {
            current = current.AddMinutes(remaining);
            // Küsurat eklenince gün değişip hafta sonuna taşarsa, hafta içine devret:
            while (!IsWorkingDay(DateOnly.FromDateTime(current.DateTime)))
            {
                current = current.AddDays(1);
            }
        }

        return current;
    }

    public decimal ToDisplayWorkingDays(long minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        return (decimal)minutes / _dailyWorkingMinutes;
    }
}
