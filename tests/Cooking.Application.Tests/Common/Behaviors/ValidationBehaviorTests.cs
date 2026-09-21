using Cooking.Application.Common.Behaviors;
using Cooking.Application.Common.Errors;
using FluentResults;
using FluentValidation;
using MediatR;

namespace Cooking.Application.Tests.Common.Behaviors;

public record FakeCommand(string Name) : IRequest<Result>;

public class FakeCommandValidator : AbstractValidator<FakeCommand>
{
    public FakeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithInvalidRequest_ReturnsValidationErrorAndDoesNotCallNext()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result>([new FakeCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new FakeCommand(""), _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        }, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.Validation, error.Code);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNextAndReturnsItsResult()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result>([new FakeCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new FakeCommand("ok"), _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithNoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result>([]);
        var nextCalled = false;

        await behavior.Handle(new FakeCommand(""), _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok());
        }, CancellationToken.None);

        Assert.True(nextCalled);
    }
}
