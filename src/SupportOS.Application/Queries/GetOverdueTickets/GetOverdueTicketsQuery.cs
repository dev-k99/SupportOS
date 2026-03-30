using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;

namespace SupportOS.Application.Queries.GetOverdueTickets;

public record GetOverdueTicketsQuery : IRequest<Result<List<TicketSummaryDto>>>;
