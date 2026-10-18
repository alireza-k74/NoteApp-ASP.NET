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
        Assert.False(note.IsDeleted);
        Assert.False(note.IsPinned);
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

        var page = await response.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Bob note", page.Items[0].Title);
    }

    [Fact]
    public async Task UpdateAndSoftDeleteNote_WorksForOwner()
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

        var listResponse = await _client.GetAsync("/api/notes");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(0, list!.TotalCount);
    }

    [Fact]
    public async Task Notes_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/notes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchAndPagination_Works()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Alpha recipe",
            Content = "eggs"
        });
        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Beta tasks",
            Content = "buy milk"
        });
        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Gamma ideas",
            Content = "recipe for cake"
        });

        var searchResponse = await _client.GetAsync("/api/notes?q=recipe");
        var search = await searchResponse.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(2, search!.TotalCount);

        var pageResponse = await _client.GetAsync("/api/notes?page=1&pageSize=2");
        var page = await pageResponse.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    public async Task PinToggle_AndPinnedFilter_Work()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Important",
            Content = "pin me"
        });
        var note = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);

        var pinResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/notes/{note!.Id}/pin"));

        Assert.Equal(HttpStatusCode.OK, pinResponse.StatusCode);
        var pinned = await pinResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);
        Assert.True(pinned!.IsPinned);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Normal",
            Content = "no pin"
        });

        var filtered = await _client.GetAsync("/api/notes?pinned=true");
        var page = await filtered.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(1, page!.TotalCount);
        Assert.Equal("Important", page.Items[0].Title);
    }

    [Fact]
    public async Task SoftDelete_Restore_AndTrash_Work()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Temp",
            Content = "gone soon"
        });
        var note = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);

        await _client.DeleteAsync($"/api/notes/{note!.Id}");

        var trashResponse = await _client.GetAsync("/api/notes/trash");
        var trash = await trashResponse.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(1, trash!.TotalCount);
        Assert.True(trash.Items[0].IsDeleted);

        var restoreResponse = await _client.PostAsync($"/api/notes/{note.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        var restored = await restoreResponse.Content.ReadFromJsonAsync<NoteResponse>(_jsonOptions);
        Assert.False(restored!.IsDeleted);
        Assert.Null(restored.DeletedAt);

        var list = await _client.GetFromJsonAsync<PagedNotesResponse>("/api/notes", _jsonOptions);
        Assert.Equal(1, list!.TotalCount);
    }

    [Fact]
    public async Task Tags_AssignFilterAndDelete_Work()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        var workTagResponse = await _client.PostAsJsonAsync("/api/tags", new CreateTagRequest
        {
            Name = "work"
        });
        Assert.Equal(HttpStatusCode.Created, workTagResponse.StatusCode);
        var workTag = await workTagResponse.Content.ReadFromJsonAsync<TagResponse>(_jsonOptions);

        var homeTagResponse = await _client.PostAsJsonAsync("/api/tags", new CreateTagRequest
        {
            Name = "home"
        });
        var homeTag = await homeTagResponse.Content.ReadFromJsonAsync<TagResponse>(_jsonOptions);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Meeting",
            Content = "standup",
            TagIds = [workTag!.Id]
        });
        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Groceries",
            Content = "milk",
            TagIds = [homeTag!.Id]
        });

        var filtered = await _client.GetAsync("/api/notes?tag=work");
        var page = await filtered.Content.ReadFromJsonAsync<PagedNotesResponse>(_jsonOptions);
        Assert.Equal(1, page!.TotalCount);
        Assert.Equal("Meeting", page.Items[0].Title);
        Assert.Contains(page.Items[0].Tags, t => t.Name == "work");

        var tagsResponse = await _client.GetAsync("/api/tags");
        var tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagResponse>>(_jsonOptions);
        Assert.Equal(2, tags!.Count);

        var deleteTag = await _client.DeleteAsync($"/api/tags/{workTag.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTag.StatusCode);

        var afterDelete = await _client.GetFromJsonAsync<List<TagResponse>>("/api/tags", _jsonOptions);
        Assert.NotNull(afterDelete);
        Assert.Single(afterDelete);
        Assert.Equal("home", afterDelete[0].Name);
    }

    [Fact]
    public async Task CreateNote_WithForeignTag_ReturnsBadRequest()
    {
        var alice = await AuthTestHelper.RegisterAsync(_client, "alice", "alice@example.com");
        AuthTestHelper.Authenticate(_client, alice.AccessToken);

        var tagResponse = await _client.PostAsJsonAsync("/api/tags", new CreateTagRequest
        {
            Name = "secret"
        });
        var tag = await tagResponse.Content.ReadFromJsonAsync<TagResponse>(_jsonOptions);

        _client.DefaultRequestHeaders.Authorization = null;
        var bob = await AuthTestHelper.RegisterAsync(_client, "bob", "bob@example.com");
        AuthTestHelper.Authenticate(_client, bob.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Hack",
            Content = "nope",
            TagIds = [tag!.Id]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTag_Duplicate_ReturnsBadRequest()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        await _client.PostAsJsonAsync("/api/tags", new CreateTagRequest { Name = "work" });
        var duplicate = await _client.PostAsJsonAsync("/api/tags", new CreateTagRequest { Name = "work" });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task PinnedNotes_AppearBeforeUnpinned()
    {
        var tokens = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, tokens.AccessToken);

        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Older pinned",
            Content = "a",
            IsPinned = true
        });
        await Task.Delay(20);
        await _client.PostAsJsonAsync("/api/notes", new CreateNoteRequest
        {
            Title = "Newer unpinned",
            Content = "b"
        });

        var page = await _client.GetFromJsonAsync<PagedNotesResponse>("/api/notes", _jsonOptions);
        Assert.Equal("Older pinned", page!.Items[0].Title);
        Assert.Equal("Newer unpinned", page.Items[1].Title);
    }
}
