using FluentAssertions;
using MediatR;
using Moq;
using SupportOS.Application.Commands.UpdateTicketStatus;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class UpdateTicketStatusCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly UpdateTicketStatusCommandHandler _handler;

    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();

    public UpdateTicketStatusCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisherMock.Setup(x => x.Publish(It.IsAny<TicketStatusChangedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _handler = new UpdateTicketStatusCommandHandler(
            _ticketRepoMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatus_WhenValid()
    {
        var ticket = new Ticket { Id = _ticketId, Status = TicketStatus.Open };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _handler.Handle(
            new UpdateTicketStatusCommand(_ticketId, TicketStatus.InProgress, _agentId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public async Task Handle_ShouldSetFirstResponseAt_WhenFirstAgentInteraction()
    {
        var ticket = new Ticket { Id = _ticketId, Status = TicketStatus.Open, FirstResponseAt = null };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _handler.Handle(
            new UpdateTicketStatusCommand(_ticketId, TicketStatus.InProgress, _agentId, Guid.NewGuid()),
            CancellationToken.None);

        ticket.FirstResponseAt.Should().NotBeNull();
        ticket.FirstResponseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSetResolvedAt_WhenStatusIsResolved()
    {
        var ticket = new Ticket { Id = _ticketId, Status = TicketStatus.InProgress };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _handler.Handle(
            new UpdateTicketStatusCommand(_ticketId, TicketStatus.Resolved, _agentId, Guid.NewGuid()),
            CancellationToken.None);

        ticket.ResolvedAt.Should().NotBeNull();
        ticket.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTicketNotFound()
    {
        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync((Ticket?)null);

        var result = await _handler.Handle(
            new UpdateTicketStatusCommand(_ticketId, TicketStatus.InProgress, _agentId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
