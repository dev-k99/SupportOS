using FluentAssertions;
using Moq;
using SupportOS.Application.Commands.AddComment;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Tests.Handlers;

public class AddCommentCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AddCommentCommandHandler _handler;

    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public AddCommentCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new AddCommentCommandHandler(
            _ticketRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddComment_WhenBodyIsValid()
    {
        var ticket = new Ticket { Id = _ticketId, Comments = new List<Comment>() };
        var author = new User { Id = _customerId, Name = "Customer", Role = UserRole.Customer };

        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(_customerId, It.IsAny<CancellationToken>())).ReturnsAsync(author);

        var result = await _handler.Handle(
            new AddCommentCommand(_ticketId, _customerId, "This is my comment.", false, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Body.Should().Be("This is my comment.");
        ticket.Comments.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldSetFirstResponseAt_WhenAgentCommentAndFirstResponseIsNull()
    {
        var ticket = new Ticket { Id = _ticketId, FirstResponseAt = null, Comments = new List<Comment>() };
        var agent = new User { Id = _agentId, Name = "Agent", Role = UserRole.Agent };

        _ticketRepoMock.Setup(r => r.GetByIdAsync(_ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(_agentId, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        await _handler.Handle(
            new AddCommentCommand(_ticketId, _agentId, "Working on it.", false, Guid.NewGuid()),
            CancellationToken.None);

        ticket.FirstResponseAt.Should().NotBeNull();
        ticket.FirstResponseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
