using FluentAssertions;
using MediatR;
using Moq;
using SupportOS.Application.Commands.AssignTicket;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class AssignTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly AssignTicketCommandHandler _handler;

    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public AssignTicketCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisherMock.Setup(x => x.Publish(It.IsAny<TicketAssignedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _handler = new AssignTicketCommandHandler(
            _ticketRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAssignTicket_WhenAgentExists()
    {
        var ticket = new Ticket { Id = _ticketId, Title = "Test", CustomerId = _customerId };
        var agent = new User { Id = _agentId, Name = "Agent", Role = UserRole.Agent };

        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(_agentId, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var result = await _handler.Handle(new AssignTicketCommand(_ticketId, _agentId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.AssignedAgentId.Should().Be(_agentId);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAgentNotFound()
    {
        var ticket = new Ticket { Id = _ticketId };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(_agentId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new AssignTicketCommand(_ticketId, _agentId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAgent()
    {
        var ticket = new Ticket { Id = _ticketId };
        var customer = new User { Id = _customerId, Role = UserRole.Customer };

        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(_customerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _handler.Handle(new AssignTicketCommand(_ticketId, _customerId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not an agent");
    }
}
