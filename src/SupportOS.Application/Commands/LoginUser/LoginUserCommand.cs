using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;

namespace SupportOS.Application.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<Result<AuthDto>>;
