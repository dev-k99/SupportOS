using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Events;
using SupportOS.Domain.Interfaces;
using SupportOS.Domain.Services;

namespace SupportOS.Application.Commands.CreateTicket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<TicketSummaryDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<TicketSummaryDto>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            CategoryId = request.CategoryId,
            CustomerId = request.CustomerId,
            SLADueAt = SLACalculator.CalculateDueDate(request.Priority, now),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new TicketCreatedEvent(ticket.Id, ticket.CustomerId, ticket.Priority), cancellationToken);

        var dto = new TicketSummaryDto(
            ticket.Id,
            ticket.Title,
            ticket.Status,
            ticket.Priority,
            string.Empty,
            string.Empty,
            null,
            ticket.SLADueAt,
            DateTime.UtcNow > ticket.SLADueAt,
            ticket.CreatedAt);

        return Result<TicketSummaryDto>.Success(dto);
    }
}
