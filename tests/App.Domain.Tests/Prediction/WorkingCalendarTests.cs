using App.Domain.Prediction;
using App.Domain.Tests.Abstractions;

namespace App.Domain.Tests.Prediction;

public class WorkingCalendarTests
{
    private const long DailyWorkingMinutes = 480;
    private readonly WorkingCalendar _calendar = new(DailyWorkingMinutes);

    [Fact]
    public void Constructor_WithZeroOrNegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkingCalendar(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkingCalendar(-1));
    }

    [Theory]
    [InlineData("2026-08-01", false)] // Saturday
    [InlineData("2026-08-02", false)] // Sunday
    [InlineData("2026-08-03", true)]  // Monday
    [InlineData("2026-08-07", true)]  // Friday
    public void IsWorkingDay_ReturnsCorrectResult(string dateString, bool expected)
    {
        var date = DateOnly.Parse(dateString);
        var result = _calendar.IsWorkingDay(date);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_WithNegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calendar.AddWorkingMinutes(DateTimeOffset.UtcNow, -1));
    }

    [Fact]
    public void AddWorkingMinutes_ZeroMinutes_ReturnsStartTime()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero)); // Monday
        var start = clock.UtcNow;

        var result = _calendar.AddWorkingMinutes(start, 0);

        Assert.Equal(start, result);
    }

    [Fact]
    public void AddWorkingMinutes_FromMonday_Adds480Minutes_ReturnsTuesdaySameTime()
    {
        var start = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero); // Monday 10:00
        var expected = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero); // Tuesday 10:00

        var result = _calendar.AddWorkingMinutes(start, 480);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FromFriday_Adds480Minutes_SkipsWeekend_ReturnsMondaySameTime()
    {
        var start = new DateTimeOffset(2026, 8, 7, 14, 0, 0, TimeSpan.Zero); // Friday 14:00
        var expected = new DateTimeOffset(2026, 8, 10, 14, 0, 0, TimeSpan.Zero); // Monday 14:00

        var result = _calendar.AddWorkingMinutes(start, 480);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FromFriday_Adds960Minutes_SkipsWeekend_ReturnsTuesdaySameTime()
    {
        var start = new DateTimeOffset(2026, 8, 7, 10, 0, 0, TimeSpan.Zero); // Friday 10:00
        var expected = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero); // Tuesday 10:00

        var result = _calendar.AddWorkingMinutes(start, 960);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_StartOnWeekend_StartsFromMonday()
    {
        var start = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero); // Saturday 10:00
        var expected = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero); // Tuesday 10:00 (Starts from Mon, adds 1 day)

        var result = _calendar.AddWorkingMinutes(start, 480);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FractionalDay_FromFriday_SkipsToMonday()
    {
        // 480 is full day. Let's say we add 24 hours (1440 mins).
        // Since we process daily_working_minutes (480), it means we advance 3 working days.
        var start = new DateTimeOffset(2026, 8, 7, 10, 0, 0, TimeSpan.Zero); // Friday
        var expected = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero); // Wednesday
        
        var result = _calendar.AddWorkingMinutes(start, 1440);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FractionalDay_AddingMinutesOvernight()
    {
        // Start Thursday 23:00. Add 120 minutes -> Friday 01:00
        var start = new DateTimeOffset(2026, 8, 6, 23, 0, 0, TimeSpan.Zero); // Thursday 23:00
        var expected = new DateTimeOffset(2026, 8, 7, 1, 0, 0, TimeSpan.Zero); // Friday 01:00

        var result = _calendar.AddWorkingMinutes(start, 120);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FractionalDay_AddingMinutesToWeekend()
    {
        // Start Friday 23:00. Add 120 minutes -> Saturday 01:00 -> Shifts to Monday 01:00
        var start = new DateTimeOffset(2026, 8, 7, 23, 0, 0, TimeSpan.Zero); // Friday 23:00
        var expected = new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero); // Monday 01:00

        var result = _calendar.AddWorkingMinutes(start, 120);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(480, 1.0)]
    [InlineData(960, 2.0)]
    [InlineData(240, 0.5)]
    [InlineData(0, 0.0)]
    public void ToDisplayWorkingDays_ReturnsCorrectDecimal(long minutes, decimal expectedDays)
    {
        var result = _calendar.ToDisplayWorkingDays(minutes);
        
        Assert.Equal(expectedDays, result);
    }

    [Fact]
    public void ToDisplayWorkingDays_WithNegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calendar.ToDisplayWorkingDays(-1));
    }
}
