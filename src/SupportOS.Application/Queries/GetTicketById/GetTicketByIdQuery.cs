using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;

namespace SupportOS.Application.Queries.GetTicketById;

public record GetTicketByIdQuery(Guid TicketId, Guid RequestingUserId, UserRole RequesterRole)
    : IRequest<Result<TicketDetailDto>>;
