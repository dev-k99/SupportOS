using FluentAssertions;
using Moq;
using SupportOS.Application.Queries.GetOverdueTickets;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class GetOverdueTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly GetOverdueTicketsQueryHandler _handler;

    public GetOverdueTicketsQueryHandlerTests()
    {
        _handler = new GetOverdueTicketsQueryHandler(_ticketRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnOverdueTickets_WhenSLABreached()
    {
        var overdueTickets = new List<Ticket>
        {
            new Ticket
            {
                Id = Guid.NewGuid(),
                Title = "Overdue ticket",
                Status = TicketStatus.Open,
                Priority = Priority.High,
                SLADueAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Category = new Category { Name = "Hardware" },
                Customer = new User { Name = "Customer" }
            }
        };

        _ticketRepoMock.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overdueTickets);

        var result = await _handler.Handle(new GetOverdueTicketsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotReturnResolvedTickets_EvenIfSLABreached()
    {
        var tickets = new List<Ticket>
        {
            new Ticket
            {
                Id = Guid.NewGuid(),
                Title = "Resolved ticket",
                Status = TicketStatus.Resolved,
                Priority = Priority.Medium,
                SLADueAt = DateTime.UtcNow.AddHours(-5),
                ResolvedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Category = new Category { Name = "Software" },
                Customer = new User { Name = "Customer" }
            }
        };

        _ticketRepoMock.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets);

        var result = await _handler.Handle(new GetOverdueTicketsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
