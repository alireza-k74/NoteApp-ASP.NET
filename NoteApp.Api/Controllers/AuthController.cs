using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteApp.Api.DTOs;
using NoteApp.Api.Services;

namespace NoteApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request)
    {
        try
        {
            var result =
                await _authService.RefreshTokenAsync(request.RefreshToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request)
    {
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdValue) ||
            !int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new
            {
                message = "Invalid token."
            });
        }

        var user = await _authService.GetCurrentUserAsync(userId);

        if (user == null)
            return NotFound(new
            {
                message = "User not found."
            });

        return Ok(user);
    }
}
