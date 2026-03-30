using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;

namespace SupportOS.Application.Commands.AddComment;

public record AddCommentCommand(
    Guid TicketId,
    Guid AuthorId,
    string Body,
    bool IsInternal,
    Guid IdempotencyKey) : IRequest<Result<CommentDto>>, IIdempotentCommand;
