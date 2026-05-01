using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperDateTimeTests
{
    [Test]
    public async Task ToDateTime_ConvertsTimestamp()
    {
        var result = Helper.ToDateTime(0);
        await Assert.That(result).IsEqualTo(new DateTime(1970, 1, 1, 8, 0, 0));
    }

    [Test]
    public async Task GetWeekAmount_ReturnsWeekCount()
    {
        var result = new DateTime(2024, 1, 1).GetWeekAmount();
        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    public async Task WeekOfYear_ReturnsWeekNumber()
    {
        var result = new DateTime(2024, 6, 15).WeekOfYear();
        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCurrentWeek_ReturnsDateTimeRange()
    {
        var result = DateTime.Now.GetCurrentWeek();
        await Assert.That(result.Start).IsLessThanOrEqualTo(result.End);
    }

    [Test]
    public async Task GetCurrentMonth_ReturnsDateTimeRange()
    {
        var result = DateTime.Now.GetCurrentMonth();
        await Assert.That(result.Start).IsLessThanOrEqualTo(result.End);
    }

    [Test]
    public async Task GetCurrentYear_ReturnsDateTimeRange()
    {
        var result = DateTime.Now.GetCurrentYear();
        await Assert.That(result.Start.Year).IsEqualTo(DateTime.Now.Year);
    }

    [Test]
    public async Task GetCurrentQuarter_ReturnsDateTimeRange()
    {
        var result = DateTime.Now.GetCurrentQuarter();
        await Assert.That(result.Start).IsLessThanOrEqualTo(result.End);
    }

    [Test]
    public async Task GetDateTime_ReturnsFormattedString()
    {
        var result = DateTime.Now.GetDateTime(0);
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task GetTotalSeconds_ReturnsTimestamp()
    {
        var result = new DateTime(1970, 1, 1, 8, 0, 0).GetTotalSeconds();
        await Assert.That(result).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GetDaysOfYear_ReturnsvalidDayCount()
    {
        var result = new DateTime(2024, 1, 1).GetDaysOfYear();
        await Assert.That(result >= 365).IsTrue();
    }

    [Test]
    public async Task GetDaysOfMonth_ReturnsValidDayCount()
    {
        var result = new DateTime(2024, 2, 1).GetDaysOfMonth();
        await Assert.That(result >= 28).IsTrue();
    }

    [Test]
    public async Task GetWeekNameOfDay_ReturnsChineseWeekName()
    {
        var result = new DateTime(2024, 1, 1).GetWeekNameOfDay();
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task In_WithCloseMode_ReturnsTrue()
    {
        var result = new DateTime(2024, 6, 15).In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task In_WithOpenMode_ReturnsFalseWhenAtBoundary()
    {
        var date = new DateTime(2024, 1, 1);
        var result = date.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), Helper.RangeMode.Open);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetMonthLastDate_ReturnsLastDayOfMonth()
    {
        var result = new DateTime(2024, 2, 1).GetMonthLastDate();
        await Assert.That(result >= 28).IsTrue();
    }

    [Test]
    public async Task DateDiff_ReturnsTimeDifferenceString()
    {
        var result = DateTime.Now.DateDiff(DateTime.Now.AddDays(-5));
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task GetDiffTime_ReturnsTimeDifferenceString()
    {
        var result = DateTime.Now.GetDiffTime(DateTime.Now.AddHours(2));
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }
}
