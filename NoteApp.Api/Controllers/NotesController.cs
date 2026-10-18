using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteApp.Api.Data;
using NoteApp.Api.DTOs;
using NoteApp.Api.Models;

namespace NoteApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotes()
    {
        var userId = GetUserId();

        var notes = await _db.Notes
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new NoteResponse
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetNote(int id)
    {
        var userId = GetUserId();

        var note = await _db.Notes
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new NoteResponse
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (note == null)
            return NotFound(new
            {
                message = "Note not found."
            });

        return Ok(note);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote(
        CreateNoteRequest request)
    {
        var userId = GetUserId();

        var note = new Note
        {
            Title = request.Title,
            Content = request.Content,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Notes.Add(note);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetNote),
            new { id = note.Id },
            new NoteResponse
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateNote(
        int id,
        UpdateNoteRequest request)
    {
        var userId = GetUserId();

        var note = await _db.Notes
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId);

        if (note == null)
            return NotFound(new
            {
                message = "Note not found."
            });

        note.Title = request.Title;
        note.Content = request.Content;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new NoteResponse
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var userId = GetUserId();

        var note = await _db.Notes
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId);

        if (note == null)
            return NotFound(new
            {
                message = "Note not found."
            });

        _db.Notes.Remove(note);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        return int.Parse(userId);
    }
}