using MediatR;
using SupportOS.Application.Commands.LoginUser;
using SupportOS.Application.Commands.RegisterUser;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;
using SupportOS.API.Extensions;

namespace SupportOS.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Authentication").RequireRateLimiting("auth");

        group.MapPost("/register", async (RegisterRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var key = GetIdempotencyKey(ctx);
            var command = new RegisterUserCommand(body.Name, body.Email, body.Password, body.Role, key);
            var result = await mediator.Send(command);
            return result.ToHttpResult<UserDto>(StatusCodes.Status201Created);
        })
        .Produces<UserDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register a new user")
        .WithDescription("Creates a new user account. Send X-Idempotency-Key header to prevent duplicate registrations on retry.");

        group.MapPost("/login", async (LoginUserCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            if (!result.IsSuccess)
                return Results.Unauthorized();
            return result.ToHttpResult<AuthDto>(StatusCodes.Status200OK);
        })
        .Produces<AuthDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithSummary("Login")
        .WithDescription("Authenticates a user and returns a JWT token.");
    }

    private static Guid GetIdempotencyKey(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Idempotency-Key", out var value)
            && Guid.TryParse(value, out var key))
            return key;

        return Guid.NewGuid();
    }

    private record RegisterRequest(string Name, string Email, string Password, UserRole Role);
}
