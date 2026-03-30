using System.Security.Claims;
using MediatR;
using SupportOS.Application.DTOs;
using SupportOS.Application.Queries.GetDashboardMetrics;
using SupportOS.Application.Queries.GetOverdueTickets;
using SupportOS.Domain.Enums;
using SupportOS.API.Extensions;

namespace SupportOS.API.Endpoints;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/metrics")
            .WithTags("Metrics")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/dashboard", async (HttpContext ctx, IMediator mediator) =>
        {
            var role = GetRole(ctx);
            if (role != UserRole.Admin) return Results.Forbid();

            var result = await mediator.Send(new GetDashboardMetricsQuery());
            return result.ToHttpResult<DashboardMetricsDto>(StatusCodes.Status200OK);
        })
        .Produces<DashboardMetricsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithSummary("Get dashboard metrics (Admin only)");

        group.MapGet("/overdue", async (HttpContext ctx, IMediator mediator) =>
        {
            var role = GetRole(ctx);
            if (role == UserRole.Customer) return Results.Forbid();

            var result = await mediator.Send(new GetOverdueTicketsQuery());
            return result.ToHttpResult<List<TicketSummaryDto>>(StatusCodes.Status200OK);
        })
        .Produces<List<TicketSummaryDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithSummary("Get overdue tickets (Agent/Admin only)");
    }

    private static UserRole GetRole(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(claim, out var role) ? role : UserRole.Customer;
    }
}
