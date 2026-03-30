using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Services;

public static class SLACalculator
{
    public static DateTime CalculateDueDate(Priority priority, DateTime createdAt)
    {
        int hours = priority switch
        {
            Priority.Low => 48,
            Priority.Medium => 24,
            Priority.High => 8,
            Priority.Critical => 2,
            _ => 24
        };

        return createdAt.AddHours(hours);
    }
}
