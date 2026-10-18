using System.ComponentModel.DataAnnotations;

namespace NoteApp.Api.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
