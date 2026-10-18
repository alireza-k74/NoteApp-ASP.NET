using NoteApp.Api.DTOs;

namespace NoteApp.Api.Services;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request);

    Task<TokenResponse> LoginAsync(LoginRequest request);

    Task<TokenResponse> RefreshTokenAsync(string refreshToken);

    Task LogoutAsync(string refreshToken);

    Task<UserResponse?> GetCurrentUserAsync(int userId);
}
