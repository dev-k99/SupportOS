using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using SupportOS.Application.Behaviors;
using SupportOS.Application.Common;

namespace SupportOS.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private record TestCommand(string Value) : IRequest<Result<string>>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .MinimumLength(3).WithMessage("Value must be at least 3 characters.");
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenValidationFails()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);

        var command = new TestCommand("");
        var next = new RequestHandlerDelegate<Result<string>>(() => Task.FromResult(Result<string>.Success("ok")));

        var result = await behavior.Handle(command, next, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(SupportOS.Domain.Common.ErrorCode.ValidationFailed);
        result.ValidationErrors.Should().ContainKey("Value");
        result.ValidationErrors!["Value"].Should().Contain("Value is required.");
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);

        var command = new TestCommand("ValidValue");
        var nextCalled = false;
        var next = new RequestHandlerDelegate<Result<string>>(() =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("ok"));
        });

        var result = await behavior.Handle(command, next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }
}
