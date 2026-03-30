using MediatR;
using SupportOS.Application.Common;
using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;
using SupportOS.Domain.Services;

namespace SupportOS.Application.Commands.EscalateTicket;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public EscalateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(EscalateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure("Ticket not found.", ErrorCode.NotFound);

        if (ticket.Priority == Priority.Critical)
            return Result.Failure("Ticket is already at Critical priority.", ErrorCode.InvalidOperation);

        var oldPriority = ticket.Priority;
        var newPriority = oldPriority + 1;

        ticket.Priority = newPriority;
        // SLA is recalculated from the original creation time, not from now.
        // Escalating should tighten the deadline relative to when the ticket was opened,
        // not grant a fresh window from the current moment.
        ticket.SLADueAt = SLACalculator.CalculateDueDate(newPriority, ticket.CreatedAt);
        ticket.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TicketEscalatedEvent(ticket.Id, oldPriority, newPriority), cancellationToken);

        return Result.Success();
    }
}
