using System.Security.Claims;
using MediatR;
using SupportOS.Application.Commands.AddComment;
using SupportOS.Application.Commands.AssignTicket;
using SupportOS.Application.Commands.CloseTicket;
using SupportOS.Application.Commands.CreateTicket;
using SupportOS.Application.Commands.EscalateTicket;
using SupportOS.Application.Commands.UpdateTicketStatus;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Application.Queries.GetTicketById;
using SupportOS.Application.Queries.GetTickets;
using SupportOS.Domain.Enums;
using SupportOS.API.Extensions;

namespace SupportOS.API.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets")
            .WithTags("Tickets")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/", async (CreateTicketRequest req, HttpContext ctx, IMediator mediator) =>
        {
            var userId = GetUserId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var command = new CreateTicketCommand(req.Title, req.Description, req.Priority, req.CategoryId, userId, GetIdempotencyKey(ctx));
            var result = await mediator.Send(command);
            return result.ToHttpResult<TicketSummaryDto>(StatusCodes.Status201Created);
        })
        .Produces<TicketSummaryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Create a ticket");

        group.MapGet("/", async (
            TicketStatus? status,
            Priority? priority,
            int page,
            int pageSize,
            HttpContext ctx,
            IMediator mediator) =>
        {
            var userId = GetUserId(ctx);
            var role = GetRole(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var query = new GetTicketsQuery(status, priority, userId, role,
                page < 1 ? 1 : page,
                pageSize < 1 ? 10 : Math.Min(pageSize, 50));
            var result = await mediator.Send(query);
            return result.ToHttpResult<PagedResult<TicketSummaryDto>>(StatusCodes.Status200OK);
        })
        .Produces<PagedResult<TicketSummaryDto>>(StatusCodes.Status200OK)
        .WithSummary("Get tickets (paginated, role-filtered)");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator) =>
        {
            var userId = GetUserId(ctx);
            var role = GetRole(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(new GetTicketByIdQuery(id, userId, role));
            return result.ToHttpResult<TicketDetailDto>(StatusCodes.Status200OK);
        })
        .Produces<TicketDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get ticket by ID");

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateStatusRequest req, HttpContext ctx, IMediator mediator) =>
        {
            var userId = GetUserId(ctx);
            var role = GetRole(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();
            if (role == UserRole.Customer) return Results.Forbid();

            var result = await mediator.Send(new UpdateTicketStatusCommand(id, req.NewStatus, userId, GetIdempotencyKey(ctx)));
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Update ticket status (Agent/Admin)");

        group.MapPatch("/{id:guid}/assign", async (Guid id, AssignRequest req, HttpContext ctx, IMediator mediator) =>
        {
            var role = GetRole(ctx);
            if (role != UserRole.Admin) return Results.Forbid();

            var result = await mediator.Send(new AssignTicketCommand(id, req.AgentId, GetIdempotencyKey(ctx)));
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Assign ticket to agent (Admin only)");

        group.MapPatch("/{id:guid}/escalate", async (Guid id, HttpContext ctx, IMediator mediator) =>
        {
            var role = GetRole(ctx);
            if (role != UserRole.Admin) return Results.Forbid();

            var result = await mediator.Send(new EscalateTicketCommand(id, GetIdempotencyKey(ctx)));
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Escalate ticket priority (Admin only)");

        group.MapPost("/{id:guid}/comments", async (Guid id, AddCommentRequest req, HttpContext ctx, IMediator mediator) =>
        {
            var userId = GetUserId(ctx);
            var role = GetRole(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var isInternal = req.IsInternal && role != UserRole.Customer;

            var command = new AddCommentCommand(id, userId, req.Body, isInternal, GetIdempotencyKey(ctx));
            var result = await mediator.Send(command);
            return result.ToHttpResult<CommentDto>(StatusCodes.Status201Created);
        })
        .Produces<CommentDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Add a comment to a ticket");

        group.MapDelete("/{id:guid}/close", async (Guid id, HttpContext ctx, IMediator mediator) =>
        {
            var role = GetRole(ctx);
            if (role != UserRole.Admin) return Results.Forbid();

            var result = await mediator.Send(new CloseTicketCommand(id, GetIdempotencyKey(ctx)));
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Close a resolved ticket (Admin only)");
    }

    private static Guid GetUserId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static UserRole GetRole(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(claim, out var role) ? role : UserRole.Customer;
    }

    private static Guid GetIdempotencyKey(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Idempotency-Key", out var value)
            && Guid.TryParse(value, out var key))
            return key;
        return Guid.NewGuid();
    }

    private record CreateTicketRequest(string Title, string Description, Priority Priority, Guid CategoryId);
    private record UpdateStatusRequest(TicketStatus NewStatus);
    private record AssignRequest(Guid AgentId);
    private record AddCommentRequest(string Body, bool IsInternal);
}
