using SupportOS.Domain.Enums;

namespace SupportOS.Application.DTOs;

public record UserDto(Guid Id, string Name, string Email, UserRole Role);
