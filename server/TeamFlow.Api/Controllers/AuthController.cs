using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Common;
using TeamFlow.Application.Auth;
using TeamFlow.Application.Auth.Dtos;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _current;
    private readonly IValidator<GoogleLoginRequest> _googleLoginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;

    public AuthController(
        IAuthService auth,
        ICurrentUserService current,
        IValidator<GoogleLoginRequest> googleLoginValidator,
        IValidator<RefreshRequest> refreshValidator)
    {
        _auth = auth;
        _current = current;
        _googleLoginValidator = googleLoginValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req, CancellationToken ct)
    {
        await _googleLoginValidator.EnsureValidAsync(req);
        var result = await _auth.GoogleLoginAsync(req.IdToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [HttpPost("guest")]
    public async Task<IActionResult> GuestLogin(CancellationToken ct)
    {
        var result = await _auth.GuestLoginAsync(HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        await _refreshValidator.EnsureValidAsync(req);
        var result = await _auth.RefreshAsync(req.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req, CancellationToken ct)
    {
        await _auth.LogoutAsync(req.RefreshToken, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        var me = await _auth.GetCurrentAsync(_current.UserId.Value, ct);
        return me is null ? Unauthorized() : Ok(me);
    }
}
