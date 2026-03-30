using FluentAssertions;
using SupportOS.Application.Commands.RegisterUser;
using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;

namespace SupportOS.Tests.Integration;

public class RegisterUserIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task RegisterUser_ValidCommand_CreatesUser()
    {
        var command = new RegisterUserCommand(
            Name: "Jane Smith",
            Email: "jane@test.com",
            Password: "Secure@1234",
            Role: UserRole.Customer,
            IdempotencyKey: Guid.NewGuid());

        var result = await Mediator.Send(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("jane@test.com");
        result.Value.Role.Should().Be(UserRole.Customer);
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_ReturnsAlreadyExists()
    {
        var command = new RegisterUserCommand("Alice", "alice@test.com", "Secure@1234", UserRole.Customer, Guid.NewGuid());
        await Mediator.Send(command); // first registration

        var duplicate = new RegisterUserCommand("Alice 2", "alice@test.com", "Secure@5678", UserRole.Customer, Guid.NewGuid());
        var result = await Mediator.Send(duplicate);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public async Task RegisterUser_WeakPassword_ReturnsValidationErrors()
    {
        var command = new RegisterUserCommand("Bob", "bob@test.com", "weak", UserRole.Customer, Guid.NewGuid());

        var result = await Mediator.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        result.ValidationErrors.Should().ContainKey("Password");
        result.ValidationErrors!["Password"].Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task RegisterUser_SameIdempotencyKey_ReturnsCachedResult()
    {
        var key = Guid.NewGuid();
        var command = new RegisterUserCommand("Charlie", "charlie@test.com", "Secure@1234", UserRole.Customer, key);

        var first  = await Mediator.Send(command);
        var second = await Mediator.Send(command);

        first.IsSuccess.Should().BeTrue();
        second.Value!.Id.Should().Be(first.Value!.Id);

        var count = DbContext.Users.Count(u => u.Email == "charlie@test.com");
        count.Should().Be(1);
    }
}
