using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NoteApp.Api.Data;
using NoteApp.Api.DTOs;
using NoteApp.Api.Models;

namespace NoteApp.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        AppDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(x => x.Email == request.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await _db.Users.AnyAsync(x => x.Username == request.Username))
            throw new InvalidOperationException("Username already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return await GenerateTokensAsync(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        return await GenerateTokensAsync(user);
    }

    public async Task<TokenResponse> RefreshTokenAsync(
        string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (token == null)
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");

        if (token.IsRevoked)
            throw new UnauthorizedAccessException(
                "Refresh token revoked.");

        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException(
                "Refresh token expired.");

        token.IsRevoked = true;

        await _db.SaveChangesAsync();

        return await GenerateTokensAsync(token.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (token == null)
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");

        if (token.IsRevoked)
            return;

        token.IsRevoked = true;

        await _db.SaveChangesAsync();
    }

    public async Task<UserResponse?> GetCurrentUserAsync(int userId)
    {
        return await _db.Users
            .Where(x => x.Id == userId)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email
            })
            .FirstOrDefaultAsync();
    }

    private async Task<TokenResponse> GenerateTokensAsync(User user)
    {
        var accessTokenExpires =
            DateTime.UtcNow.AddMinutes(
                int.Parse(
                    _configuration["Jwt:AccessTokenMinutes"]!));

        var refreshTokenExpires =
            DateTime.UtcNow.AddDays(
                int.Parse(
                    _configuration["Jwt:RefreshTokenDays"]!));

        var accessToken = GenerateAccessToken(
            user,
            accessTokenExpires);

        var refreshToken = GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = refreshTokenExpires
        });

        await _db.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpires,
            RefreshTokenExpiresAt = refreshTokenExpires
        };
    }

    private string GenerateAccessToken(
        User user,
        DateTime expiresAt)
    {
        var key = _configuration["Jwt:Key"]!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username)
        };

        var credentials = new SigningCredentials(
            JwtTokenHelper.CreateSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }
}
