namespace SupportOS.Application.DTOs;

public record CommentDto(Guid Id, string Body, string AuthorName, bool IsInternal, DateTime CreatedAt);
