using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TeamFlow.Application.Auth.Dtos;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Auth;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Services;

public class AuthService : Application.Auth.IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly string _googleClientId;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService,
        AppDbContext db,
        IOptions<JwtOptions> jwt,
        IConfiguration config)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _db = db;
        _jwt = jwt.Value;
        _googleClientId = config["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId missing from configuration.");
    }

    public async Task<AuthResponse> GoogleLoginAsync(string idToken, string? ip, CancellationToken ct)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_googleClientId]
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new ForbiddenException("Invalid Google token.");
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                DisplayName = payload.Name ?? payload.Email,
                AvatarUrl = payload.Picture,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray());
                throw new ValidationException(errors);
            }

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new ApplicationRole("User"));
            await _userManager.AddToRoleAsync(user, "User");
        }
        else
        {
            // Update profile from Google on each login
            user.AvatarUrl = payload.Picture;
            if (!string.IsNullOrEmpty(payload.Name))
                user.DisplayName = payload.Name;
            await _userManager.UpdateAsync(user);
        }

        return await IssueTokensAsync(user, ip, ct);
    }

    public async Task<AuthResponse> GuestLoginAsync(string? ip, CancellationToken ct)
    {
        var guest = await _userManager.FindByEmailAsync("guest@teamflow.local")
            ?? throw new ForbiddenException("Guest account not available.");

        return await IssueTokensAsync(guest, ip, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ip, CancellationToken ct)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive || stored.User is null)
            throw new ForbiddenException("Invalid refresh token.");

        // Rotate
        var (newToken, newHash) = _tokenService.CreateRefreshToken();
        var newEntity = new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip
        };
        _db.RefreshTokens.Add(newEntity);

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ip;
        stored.ReplacedByTokenId = newEntity.Id;

        await _db.SaveChangesAsync(ct);

        var roles = await _userManager.GetRolesAsync(stored.User);
        var access = _tokenService.CreateAccessToken(stored.User, roles);

        return new AuthResponse(
            access,
            newToken,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            ToUserDto(stored.User, roles));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (stored is null || !stored.IsActive) return;
        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserDto?> GetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return ToUserDto(user, roles);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ip, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var access = _tokenService.CreateAccessToken(user, roles);
        var (refresh, hash) = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip
        });
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            access,
            refresh,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            ToUserDto(user, roles));
    }

    private static UserDto ToUserDto(ApplicationUser u, IList<string> roles) =>
        new(u.Id, u.Email ?? "", u.DisplayName, u.AvatarUrl, roles.ToList());
}
