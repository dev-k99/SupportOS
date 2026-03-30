using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;

namespace SupportOS.Application.Queries.GetDashboardMetrics;

public record GetDashboardMetricsQuery : IRequest<Result<DashboardMetricsDto>>;
