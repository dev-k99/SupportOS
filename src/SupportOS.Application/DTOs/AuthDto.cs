using SupportOS.Domain.Enums;

namespace SupportOS.Application.DTOs;

public record AuthDto(string Token, DateTime ExpiresAt, Guid UserId, UserRole Role);
