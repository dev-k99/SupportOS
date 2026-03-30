using FluentValidation;

namespace SupportOS.Application.Commands.AssignTicket;

public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty().WithMessage("Ticket ID is required.");
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("Agent ID is required.");
    }
}
