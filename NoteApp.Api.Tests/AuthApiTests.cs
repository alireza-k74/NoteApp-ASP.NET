using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NoteApp.Api.DTOs;
using NoteApp.Api.Tests.Helpers;
using NoteApp.Api.Tests.Infrastructure;

namespace NoteApp.Api.Tests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ReturnsTokens()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.True(tokens.RefreshTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        await AuthTestHelper.RegisterAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "bob",
            Email = "alice@example.com",
            Password = "StrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        await AuthTestHelper.RegisterAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "alice",
            Email = "bob@example.com",
            Password = "StrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ab",
            email = "not-an-email",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await AuthTestHelper.RegisterAsync(_client);

        var tokens = await AuthTestHelper.LoginAsync(_client);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        await AuthTestHelper.RegisterAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "alice@example.com",
            Password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewTokens()
    {
        var initial = await AuthTestHelper.RegisterAsync(_client);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = initial.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotated = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
        Assert.NotNull(rotated);
        Assert.NotEqual(initial.RefreshToken, rotated!.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(rotated.AccessToken));

        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = initial.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = "not-a-real-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_AlreadyRevoked_ReturnsNoContent()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);

        await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });

        var secondLogout = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.NoContent, secondLogout.StatusCode);
    }

    [Fact]
    public async Task Me_Authenticated_ReturnsCurrentUser()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        Assert.NotNull(user);
        Assert.Equal("alice", user!.Username);
        Assert.Equal("alice@example.com", user.Email);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task Me_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
