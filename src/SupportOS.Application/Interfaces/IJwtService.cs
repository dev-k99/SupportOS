using SupportOS.Domain.Entities;

namespace SupportOS.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
