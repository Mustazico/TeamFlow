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
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;

    public AuthController(
        IAuthService auth,
        ICurrentUserService current,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshRequest> refreshValidator)
    {
        _auth = auth;
        _current = current;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        await _registerValidator.EnsureValidAsync(req);
        var result = await _auth.RegisterAsync(req, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        await _loginValidator.EnsureValidAsync(req);
        var result = await _auth.LoginAsync(req, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
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
