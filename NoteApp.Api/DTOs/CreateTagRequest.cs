using System.ComponentModel.DataAnnotations;

namespace NoteApp.Api.DTOs;

public class CreateTagRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}
