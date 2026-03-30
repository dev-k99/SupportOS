using MediatR;
using SupportOS.Application.Common;
using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Commands.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure("Ticket not found.", ErrorCode.NotFound);

        if (!ticket.CanTransitionTo(request.NewStatus))
            return Result.Failure($"Cannot transition ticket from {ticket.Status} to {request.NewStatus}.", ErrorCode.InvalidOperation);

        var oldStatus = ticket.Status;
        var now = DateTime.UtcNow;

        ticket.RecordFirstResponse(now);

        ticket.Status = request.NewStatus;
        ticket.UpdatedAt = now;

        if (request.NewStatus == TicketStatus.Resolved)
            ticket.ResolvedAt = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TicketStatusChangedEvent(ticket.Id, oldStatus, request.NewStatus), cancellationToken);

        return Result.Success();
    }
}
