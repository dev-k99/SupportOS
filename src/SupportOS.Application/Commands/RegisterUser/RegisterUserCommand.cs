using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;

namespace SupportOS.Application.Commands.RegisterUser;

public record RegisterUserCommand(string Name, string Email, string Password, UserRole Role, Guid IdempotencyKey)
    : IRequest<Result<UserDto>>, IIdempotentCommand;
