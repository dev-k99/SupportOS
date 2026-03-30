using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Common;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<CommentDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddCommentCommandHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result<CommentDto>.Failure("Ticket not found.", ErrorCode.NotFound);

        var author = await _userRepository.GetByIdAsync(request.AuthorId, cancellationToken);
        if (author is null)
            return Result<CommentDto>.Failure("Author not found.", ErrorCode.NotFound);

        var now = DateTime.UtcNow;

        if (author.Role == UserRole.Agent || author.Role == UserRole.Admin)
            ticket.RecordFirstResponse(now);

        var comment = new Comment
        {
            TicketId = request.TicketId,
            AuthorId = request.AuthorId,
            Body = request.Body,
            IsInternal = request.IsInternal,
            CreatedAt = now,
            UpdatedAt = now
        };

        ticket.Comments.Add(comment);
        ticket.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CommentDto>.Success(new CommentDto(comment.Id, comment.Body, author.Name, comment.IsInternal, comment.CreatedAt));
    }
}
