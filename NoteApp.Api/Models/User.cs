namespace NoteApp.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Note> Notes { get; set; } = new List<Note>();

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}