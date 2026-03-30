using FluentAssertions;
using MediatR;
using Moq;
using SupportOS.Application.Commands.CreateTicket;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class CreateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly CreateTicketCommandHandler _handler;

    public CreateTicketCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisherMock.Setup(x => x.Publish(It.IsAny<TicketCreatedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ticketRepoMock.Setup(x => x.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _handler = new CreateTicketCommandHandler(_ticketRepoMock.Object, _unitOfWorkMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateTicket_WhenRequestIsValid()
    {
        var command = new CreateTicketCommand(
            "System is down",
            "Production system is completely down.",
            Priority.High,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("System is down");
        _ticketRepoMock.Verify(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Priority.Low, 48)]
    [InlineData(Priority.Medium, 24)]
    [InlineData(Priority.High, 8)]
    [InlineData(Priority.Critical, 2)]
    public async Task Handle_ShouldSetSLADueDate_BasedOnPriority(Priority priority, int expectedHours)
    {
        Ticket? capturedTicket = null;
        _ticketRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((t, _) => capturedTicket = t)
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var command = new CreateTicketCommand(
            "Test ticket title here",
            "Test description long enough.",
            priority,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        capturedTicket.Should().NotBeNull();
        var expectedDue = capturedTicket!.CreatedAt.AddHours(expectedHours);
        capturedTicket.SLADueAt.Should().BeCloseTo(expectedDue, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldPublishTicketCreatedEvent()
    {
        var command = new CreateTicketCommand(
            "Test ticket title",
            "Description for the test ticket.",
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<TicketCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
