using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupportOS.Application.Commands.CreateTicket;
using SupportOS.Application.Commands.RegisterUser;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Infrastructure.Persistence;
using SupportOS.Tests.Integration;

namespace SupportOS.Tests.Integration;

public class CreateTicketIntegrationTests : IntegrationTestBase
{
    private Guid _customerId;
    private Guid _categoryId;

    protected override async Task SeedBaseDataAsync()
    {
        _categoryId = Guid.NewGuid();
        DbContext.Categories.Add(new Category
        {
            Id = _categoryId,
            Name = "Hardware",
            DefaultSLAHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _customerId = Guid.NewGuid();
        DbContext.Users.Add(new SupportOS.Domain.Entities.User
        {
            Id = _customerId,
            Name = "Test Customer",
            Email = "customer@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234", 4), // low cost for tests
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateTicket_ValidCommand_PersistsTicketAndReturnsDto()
    {
        var command = new CreateTicketCommand(
            Title: "Screen flickering",
            Description: "Monitor flickers intermittently.",
            Priority: Priority.High,
            CategoryId: _categoryId,
            CustomerId: _customerId,
            IdempotencyKey: Guid.NewGuid());

        var result = await Mediator.Send(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Screen flickering");

        var persisted = await DbContext.Tickets.FirstOrDefaultAsync(t => t.Id == result.Value.Id);
        persisted.Should().NotBeNull();
        persisted!.Priority.Should().Be(Priority.High);
        persisted.SLADueAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateTicket_EmptyTitle_ReturnsValidationFailure()
    {
        var command = new CreateTicketCommand(
            Title: "",
            Description: "Valid description.",
            Priority: Priority.Medium,
            CategoryId: _categoryId,
            CustomerId: _customerId,
            IdempotencyKey: Guid.NewGuid());

        var result = await Mediator.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(SupportOS.Domain.Common.ErrorCode.ValidationFailed);
        result.ValidationErrors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task CreateTicket_SameIdempotencyKey_ReturnsCachedResult()
    {
        var key = Guid.NewGuid();
        var command = new CreateTicketCommand(
            Title: "Idempotent ticket",
            Description: "Testing idempotency.",
            Priority: Priority.Low,
            CategoryId: _categoryId,
            CustomerId: _customerId,
            IdempotencyKey: key);

        var first  = await Mediator.Send(command);
        var second = await Mediator.Send(command);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value!.Id.Should().Be(first.Value!.Id); // same result returned

        var count = await DbContext.Tickets.CountAsync(t => t.Title == "Idempotent ticket");
        count.Should().Be(1); // only one ticket created
    }
}
