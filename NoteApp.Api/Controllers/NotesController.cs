using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteApp.Api.DTOs;
using NoteApp.Api.Services;

namespace NoteApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotes([FromQuery] NoteQuery query)
    {
        var result = await _noteService.GetNotesAsync(GetUserId(), query);
        return Ok(result);
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash([FromQuery] NoteQuery query)
    {
        var result = await _noteService.GetTrashAsync(GetUserId(), query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetNote(int id)
    {
        var note = await _noteService.GetNoteAsync(GetUserId(), id);

        if (note == null)
            return NotFound(new { message = "Note not found." });

        return Ok(note);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote(CreateNoteRequest request)
    {
        try
        {
            var note = await _noteService.CreateNoteAsync(GetUserId(), request);

            return CreatedAtAction(
                nameof(GetNote),
                new { id = note.Id },
                note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateNote(int id, UpdateNoteRequest request)
    {
        try
        {
            var note = await _noteService.UpdateNoteAsync(GetUserId(), id, request);

            if (note == null)
                return NotFound(new { message = "Note not found." });

            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var deleted = await _noteService.SoftDeleteAsync(GetUserId(), id);

        if (!deleted)
            return NotFound(new { message = "Note not found." });

        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreNote(int id)
    {
        var note = await _noteService.RestoreAsync(GetUserId(), id);

        if (note == null)
            return NotFound(new { message = "Note not found in trash." });

        return Ok(note);
    }

    [HttpPatch("{id:int}/pin")]
    public async Task<IActionResult> TogglePin(int id)
    {
        var note = await _noteService.TogglePinAsync(GetUserId(), id);

        if (note == null)
            return NotFound(new { message = "Note not found." });

        return Ok(note);
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        return int.Parse(userId);
    }
}
