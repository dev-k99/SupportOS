using FluentValidation;

namespace SupportOS.Application.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MinimumLength(1).WithMessage("Comment body must be at least 1 character.")
            .MaximumLength(5000).WithMessage("Comment body must not exceed 5000 characters.");

        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("Author ID is required.");
    }
}
