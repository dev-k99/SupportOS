using MediatR;
using SupportOS.Application.Common;
using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Commands.CloseTicket;

public class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseTicketCommandHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure("Ticket not found.", ErrorCode.NotFound);

        if (!ticket.CanTransitionTo(TicketStatus.Closed))
            return Result.Failure($"Cannot close a ticket in '{ticket.Status}' status. Only resolved tickets can be closed.", ErrorCode.InvalidOperation);

        ticket.Status = TicketStatus.Closed;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
