using FluentAssertions;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Services;

namespace SupportOS.Tests.Domain;

public class SLACalculatorTests
{
    private readonly DateTime _baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CalculateDueDate_ShouldAdd48Hours_ForLowPriority()
    {
        var result = SLACalculator.CalculateDueDate(Priority.Low, _baseTime);
        result.Should().Be(_baseTime.AddHours(48));
    }

    [Fact]
    public void CalculateDueDate_ShouldAdd24Hours_ForMediumPriority()
    {
        var result = SLACalculator.CalculateDueDate(Priority.Medium, _baseTime);
        result.Should().Be(_baseTime.AddHours(24));
    }

    [Fact]
    public void CalculateDueDate_ShouldAdd8Hours_ForHighPriority()
    {
        var result = SLACalculator.CalculateDueDate(Priority.High, _baseTime);
        result.Should().Be(_baseTime.AddHours(8));
    }

    [Fact]
    public void CalculateDueDate_ShouldAdd2Hours_ForCriticalPriority()
    {
        var result = SLACalculator.CalculateDueDate(Priority.Critical, _baseTime);
        result.Should().Be(_baseTime.AddHours(2));
    }
}
