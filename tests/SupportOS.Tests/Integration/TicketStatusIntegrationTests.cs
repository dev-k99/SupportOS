using FluentAssertions;
using SupportOS.Application.Commands.AssignTicket;
using SupportOS.Application.Commands.CloseTicket;
using SupportOS.Application.Commands.CreateTicket;
using SupportOS.Application.Commands.EscalateTicket;
using SupportOS.Application.Commands.UpdateTicketStatus;
using SupportOS.Domain.Common;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;

namespace SupportOS.Tests.Integration;

public class TicketStatusIntegrationTests : IntegrationTestBase
{
    private Guid _customerId;
    private Guid _agentId;
    private Guid _categoryId;

    protected override async Task SeedBaseDataAsync()
    {
        _categoryId = Guid.NewGuid();
        DbContext.Categories.Add(new Category
        {
            Id = _categoryId, Name = "Software", DefaultSLAHours = 8,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        _customerId = Guid.NewGuid();
        _agentId    = Guid.NewGuid();

        DbContext.Users.AddRange(
            new User
            {
                Id = _customerId, Name = "Customer", Email = "c@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234", 4),
                Role = UserRole.Customer, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = _agentId, Name = "Agent", Email = "a@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234", 4),
                Role = UserRole.Agent, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });

        await DbContext.SaveChangesAsync();
    }

    private async Task<Guid> CreateTicketAsync(string title = "Test ticket")
    {
        var result = await Mediator.Send(new CreateTicketCommand(
            title, "Description", Priority.Medium, _categoryId, _customerId, Guid.NewGuid()));
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Id;
    }

    [Fact]
    public async Task UpdateStatus_OpenToInProgress_SetsFirstResponseAt()
    {
        var ticketId = await CreateTicketAsync();

        var result = await Mediator.Send(
            new UpdateTicketStatusCommand(ticketId, TicketStatus.InProgress, _agentId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        var ticket = await DbContext.Tickets.FindAsync(ticketId);
        ticket!.Status.Should().Be(TicketStatus.InProgress);
        ticket.FirstResponseAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_OpenToResolved_ReturnsFailure()
    {
        var ticketId = await CreateTicketAsync();

        // Cannot jump Open → Resolved — must go through InProgress
        var result = await Mediator.Send(
            new UpdateTicketStatusCommand(ticketId, TicketStatus.Resolved, _agentId, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.InvalidOperation);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_OpenToClosed_ReturnsFailure()
    {
        var ticketId = await CreateTicketAsync();

        var result = await Mediator.Send(
            new UpdateTicketStatusCommand(ticketId, TicketStatus.Closed, _agentId, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.InvalidOperation);
    }

    [Fact]
    public async Task AssignTicket_ToAgent_Succeeds()
    {
        var ticketId = await CreateTicketAsync();

        var result = await Mediator.Send(
            new AssignTicketCommand(ticketId, _agentId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        var ticket = await DbContext.Tickets.FindAsync(ticketId);
        ticket!.AssignedAgentId.Should().Be(_agentId);
    }

    [Fact]
    public async Task AssignTicket_ToCustomer_ReturnsFailure()
    {
        var ticketId = await CreateTicketAsync();

        var result = await Mediator.Send(
            new AssignTicketCommand(ticketId, _customerId, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.InvalidOperation);
    }

    [Fact]
    public async Task CloseTicket_WhenResolved_Succeeds()
    {
        var ticketId = await CreateTicketAsync();

        // Open → InProgress → Resolved → Closed
        await Mediator.Send(new UpdateTicketStatusCommand(ticketId, TicketStatus.InProgress, _agentId, Guid.NewGuid()));
        await Mediator.Send(new UpdateTicketStatusCommand(ticketId, TicketStatus.Resolved, _agentId, Guid.NewGuid()));

        var result = await Mediator.Send(new CloseTicketCommand(ticketId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        var ticket = await DbContext.Tickets.FindAsync(ticketId);
        ticket!.Status.Should().Be(TicketStatus.Closed);
    }

    [Fact]
    public async Task CloseTicket_WhenOpen_ReturnsFailure()
    {
        var ticketId = await CreateTicketAsync();

        var result = await Mediator.Send(new CloseTicketCommand(ticketId, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.InvalidOperation);
    }

    [Fact]
    public async Task EscalateTicket_IncreasePriority_UpdatesSLADue()
    {
        var ticketId = await CreateTicketAsync();

        var before = (await DbContext.Tickets.FindAsync(ticketId))!.SLADueAt;

        var result = await Mediator.Send(new EscalateTicketCommand(ticketId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        var ticket = await DbContext.Tickets.FindAsync(ticketId);
        ticket!.Priority.Should().Be(Priority.High); // Medium → High
        ticket.SLADueAt.Should().BeBefore(before);   // SLA tightened
    }

    [Fact]
    public async Task EscalateTicket_SLACalculatedFromCreationTime_NotFromNow()
    {
        // Ticket created as Medium → SLA due = CreatedAt + 24h
        var ticketId = await CreateTicketAsync();
        var ticket = await DbContext.Tickets.FindAsync(ticketId);
        var createdAt = ticket!.CreatedAt;

        // Escalate Medium → High (SLA = 8h from creation, not from now)
        var result = await Mediator.Send(new EscalateTicketCommand(ticketId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        ticket = await DbContext.Tickets.FindAsync(ticketId);
        var expectedSLADue = createdAt.AddHours(8);
        // Should be 8h from creation, not 8h from now
        ticket!.SLADueAt.Should().BeCloseTo(expectedSLADue, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task EscalateTicket_AlreadyCritical_ReturnsFailure()
    {
        // Create a critical ticket directly in DB
        var ticketId = Guid.NewGuid();
        DbContext.Tickets.Add(new Ticket
        {
            Id = ticketId, Title = "Critical", Description = "Already critical",
            Status = TicketStatus.Open, Priority = Priority.Critical,
            CategoryId = _categoryId, CustomerId = _customerId,
            SLADueAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        var result = await Mediator.Send(new EscalateTicketCommand(ticketId, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.InvalidOperation);
    }
}
