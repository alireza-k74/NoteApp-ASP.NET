namespace NoteApp.Api.DTOs;

public class UpdateNoteRequest
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}