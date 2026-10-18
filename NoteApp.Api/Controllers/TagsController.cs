using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteApp.Api.DTOs;
using NoteApp.Api.Services;

namespace NoteApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly INoteService _noteService;

    public TagsController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _noteService.GetTagsAsync(GetUserId());
        return Ok(tags);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag(CreateTagRequest request)
    {
        try
        {
            var tag = await _noteService.CreateTagAsync(GetUserId(), request);
            return Created($"/api/tags/{tag.Id}", tag);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var deleted = await _noteService.DeleteTagAsync(GetUserId(), id);

        if (!deleted)
            return NotFound(new { message = "Tag not found." });

        return NoContent();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        return int.Parse(userId);
    }
}
