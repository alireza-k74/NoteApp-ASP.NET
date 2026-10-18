namespace NoteApp.Api.DTOs;

public class PagedNotesResponse
{
    public List<NoteResponse> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
