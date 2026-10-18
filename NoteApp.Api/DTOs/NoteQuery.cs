using System.ComponentModel.DataAnnotations;

namespace NoteApp.Api.DTOs;

public class NoteQuery
{
    [MaxLength(200)]
    public string? Q { get; set; }

    [MaxLength(50)]
    public string? Tag { get; set; }

    public bool? Pinned { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
