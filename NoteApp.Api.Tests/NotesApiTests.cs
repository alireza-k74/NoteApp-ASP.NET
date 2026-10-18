using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NoteApp.Api.DTOs;
using NoteApp.Api.Tests.Helpers;
using NoteApp.Api.Tests.Infrastructure;

namespace NoteApp.Api.Tests;

public class NotesApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotesApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateNote_ValidRequest_ReturnsCreated()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Shopping list",
            Content = "Milk, eggs"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var note = await response.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);
        Assert.NotNull(note);
        Assert.Equal("Shopping list", note!.Title);
        Assert.Equal("Milk, eggs", note.Content);
    }

    [Fact]
    public async Task CreateNote_MissingTitle_ReturnsBadRequest()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/notes", new
        {
            title = "",
            content = "body"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNotes_ReturnsOnlyCurrentUserNotes()
    {
        var alice = await AuthTestHelper.RegisterAsync(_client, "alice", "alice@example.com");
        AuthTestHelper.Authenticate(_client, alice.AccessToken);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Alice note",
            Content = "private"
        });

        _client.DefaultRequestHeaders.Authorization = null;

        var bob = await AuthTestHelper.RegisterAsync(_client, "bob", "bob@example.com");
        AuthTestHelper.Authenticate(_client, bob.AccessToken);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Bob note",
            Content = "also private"
        });

        var response = await _client.GetAsync("/api/notes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notes = await response.Content.ReadFromJsonAsync<List<NoteResponse>>(_jsonOptions);
        Assert.NotNull(notes);
        Assert.Single(notes!);
        Assert.Equal("Bob note", notes[0].Title);
    }

    [Fact]
    public async Task UpdateAndDeleteNote_WorksForOwner()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Draft",
            Content = "v1"
        });

        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);
        Assert.NotNull(created);

        var updateResponse = await _client.PutAsJsonAsync($"/api/notes/{created!.Id}", new UpdateNoteRequest
        {
            Title = "Final",
            Content = "v2"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);
        Assert.Equal("Final", updated!.Title);
        Assert.Equal("v2", updated.Content);

        var deleteResponse = await _client.DeleteAsync($"/api/notes/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/notes/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Notes_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/notes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
