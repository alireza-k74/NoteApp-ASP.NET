using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
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
        if (_db.Users.Any(x => x.Email == request.Email))
            throw new InvalidOperationException("Email already exists.");

        if (_db.Users.Any(x => x.Username == request.Username))
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
        var user = _db.Users
            .FirstOrDefault(x => x.Email == request.Email);

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
        var token = _db.RefreshTokens
            .FirstOrDefault(x => x.Token == refreshToken);

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
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)),
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