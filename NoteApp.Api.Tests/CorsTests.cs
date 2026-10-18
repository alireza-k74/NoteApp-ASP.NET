using System.Net;
using NoteApp.Api.Tests.Infrastructure;

namespace NoteApp.Api.Tests;

public class CorsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_ReturnsCorsHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(
            "http://localhost:5173",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_DoesNotAllowOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        var hasAllowOrigin = response.Headers.TryGetValues(
            "Access-Control-Allow-Origin",
            out var values);

        Assert.True(!hasAllowOrigin || !values!.Contains("http://evil.example"));
    }
}
