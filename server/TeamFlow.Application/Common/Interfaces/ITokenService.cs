using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    (string token, string hash) CreateRefreshToken();
    string HashRefreshToken(string token);
}
