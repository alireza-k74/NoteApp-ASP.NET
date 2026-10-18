using System.ComponentModel.DataAnnotations;

namespace NoteApp.Api.DTOs;

public class UpdateNoteRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(10000)]
    public string Content { get; set; } = string.Empty;
}
