using FluentAssertions;
using Moq;
using SupportOS.Application.Queries.GetDashboardMetrics;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class GetDashboardMetricsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetDashboardMetricsQueryHandler _handler;

    private readonly Category _hwCategory = new() { Id = Guid.NewGuid(), Name = "Hardware" };
    private readonly Category _swCategory = new() { Id = Guid.NewGuid(), Name = "Software" };

    public GetDashboardMetricsQueryHandlerTests()
    {
        _handler = new GetDashboardMetricsQueryHandler(_ticketRepoMock.Object, _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectCounts()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), Status = TicketStatus.Open, Priority = Priority.High, SLADueAt = DateTime.UtcNow.AddHours(2), CreatedAt = DateTime.UtcNow, Category = _hwCategory },
            new() { Id = Guid.NewGuid(), Status = TicketStatus.InProgress, Priority = Priority.Medium, SLADueAt = DateTime.UtcNow.AddHours(5), CreatedAt = DateTime.UtcNow, Category = _swCategory },
            new() { Id = Guid.NewGuid(), Status = TicketStatus.Resolved, Priority = Priority.Low, SLADueAt = DateTime.UtcNow.AddHours(-1), CreatedAt = DateTime.UtcNow.AddDays(-1), ResolvedAt = DateTime.UtcNow.AddHours(-0.5), Category = _swCategory }
        };

        _ticketRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tickets);
        _userRepoMock.Setup(r => r.GetAgentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

        var result = await _handler.Handle(new GetDashboardMetricsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalTickets.Should().Be(3);
        result.Value.OpenTickets.Should().Be(1);
        result.Value.InProgressTickets.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldCalculateSLABreachRate_Correctly()
    {
        var now = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), Status = TicketStatus.Open, Priority = Priority.High, SLADueAt = now.AddHours(-2), CreatedAt = now.AddDays(-1), Category = _hwCategory },
            new() { Id = Guid.NewGuid(), Status = TicketStatus.Open, Priority = Priority.Medium, SLADueAt = now.AddHours(5), CreatedAt = now, Category = _swCategory },
            new() { Id = Guid.NewGuid(), Status = TicketStatus.InProgress, Priority = Priority.Low, SLADueAt = now.AddHours(10), CreatedAt = now, Category = _swCategory },
            new() { Id = Guid.NewGuid(), Status = TicketStatus.InProgress, Priority = Priority.Low, SLADueAt = now.AddHours(-3), CreatedAt = now.AddDays(-2), Category = _hwCategory }
        };

        _ticketRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tickets);
        _userRepoMock.Setup(r => r.GetAgentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

        var result = await _handler.Handle(new GetDashboardMetricsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 2 out of 4 active tickets are overdue = 50%
        result.Value!.SLABreachRate.Should().Be(50.0);
    }
}
