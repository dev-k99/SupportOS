using MediatR;
using SupportOS.Application.Common;
using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Commands.AssignTicket;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public AssignTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure("Ticket not found.", ErrorCode.NotFound);

        var agent = await _userRepository.GetByIdAsync(request.AgentId, cancellationToken);
        if (agent is null)
            return Result.Failure("Agent not found.", ErrorCode.NotFound);

        if (agent.Role != UserRole.Agent)
            return Result.Failure("The specified user is not an agent.", ErrorCode.InvalidOperation);

        ticket.AssignedAgentId = agent.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TicketAssignedEvent(ticket.Id, agent.Id), cancellationToken);

        return Result.Success();
    }
}
