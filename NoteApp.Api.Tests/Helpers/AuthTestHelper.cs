using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NoteApp.Api.DTOs;

namespace NoteApp.Api.Tests.Helpers;

public static class AuthTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<TokenResponse> RegisterAsync(
        HttpClient client,
        string username = "alice",
        string email = "alice@example.com",
        string password = "StrongPassword123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        return tokens ?? throw new InvalidOperationException("Token response was null.");
    }

    public static async Task<TokenResponse> LoginAsync(
        HttpClient client,
        string email = "alice@example.com",
        string password = "StrongPassword123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        return tokens ?? throw new InvalidOperationException("Token response was null.");
    }

    public static void Authenticate(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
