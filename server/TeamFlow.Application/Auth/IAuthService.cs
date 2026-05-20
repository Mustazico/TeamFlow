using TeamFlow.Application.Auth.Dtos;

namespace TeamFlow.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> GoogleLoginAsync(string idToken, string? ip, CancellationToken ct);
    Task<AuthResponse> GuestLoginAsync(string? ip, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ip, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
    Task<UserDto?> GetCurrentAsync(Guid userId, CancellationToken ct);
}
