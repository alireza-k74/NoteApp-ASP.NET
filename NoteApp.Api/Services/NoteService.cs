using Microsoft.EntityFrameworkCore;
using NoteApp.Api.Data;
using NoteApp.Api.DTOs;
using NoteApp.Api.Models;

namespace NoteApp.Api.Services;

public class NoteService : INoteService
{
    private readonly AppDbContext _db;

    public NoteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedNotesResponse> GetNotesAsync(int userId, NoteQuery query)
    {
        var notes = _db.Notes
            .AsNoTracking()
            .Include(x => x.Tags)
            .Where(x => x.UserId == userId && !x.IsDeleted);

        return await ToPagedResponseAsync(notes, query);
    }

    public async Task<PagedNotesResponse> GetTrashAsync(int userId, NoteQuery query)
    {
        var notes = _db.Notes
            .AsNoTracking()
            .Include(x => x.Tags)
            .Where(x => x.UserId == userId && x.IsDeleted);

        return await ToPagedResponseAsync(notes, query, trashMode: true);
    }

    public async Task<NoteResponse?> GetNoteAsync(int userId, int noteId)
    {
        var note = await _db.Notes
            .AsNoTracking()
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x =>
                x.Id == noteId &&
                x.UserId == userId &&
                !x.IsDeleted);

        return note == null ? null : MapNote(note);
    }

    public async Task<NoteResponse> CreateNoteAsync(
        int userId,
        CreateNoteRequest request)
    {
        var tags = await ResolveTagsAsync(userId, request.TagIds);

        var note = new Note
        {
            Title = request.Title.Trim(),
            Content = request.Content,
            IsPinned = request.IsPinned,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = tags
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync();

        return MapNote(note);
    }

    public async Task<NoteResponse?> UpdateNoteAsync(
        int userId,
        int noteId,
        UpdateNoteRequest request)
    {
        var note = await _db.Notes
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x =>
                x.Id == noteId &&
                x.UserId == userId &&
                !x.IsDeleted);

        if (note == null)
            return null;

        var tags = await ResolveTagsAsync(userId, request.TagIds);

        note.Title = request.Title.Trim();
        note.Content = request.Content;
        note.IsPinned = request.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;
        note.Tags.Clear();
        foreach (var tag in tags)
            note.Tags.Add(tag);

        await _db.SaveChangesAsync();

        return MapNote(note);
    }

    public async Task<bool> SoftDeleteAsync(int userId, int noteId)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(x =>
                x.Id == noteId &&
                x.UserId == userId &&
                !x.IsDeleted);

        if (note == null)
            return false;

        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<NoteResponse?> RestoreAsync(int userId, int noteId)
    {
        var note = await _db.Notes
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x =>
                x.Id == noteId &&
                x.UserId == userId &&
                x.IsDeleted);

        if (note == null)
            return null;

        note.IsDeleted = false;
        note.DeletedAt = null;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapNote(note);
    }

    public async Task<NoteResponse?> TogglePinAsync(int userId, int noteId)
    {
        var note = await _db.Notes
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x =>
                x.Id == noteId &&
                x.UserId == userId &&
                !x.IsDeleted);

        if (note == null)
            return null;

        note.IsPinned = !note.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapNote(note);
    }

    public async Task<List<TagResponse>> GetTagsAsync(int userId)
    {
        return await _db.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new TagResponse
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TagResponse> CreateTagAsync(
        int userId,
        CreateTagRequest request)
    {
        var name = request.Name.Trim();

        if (await _db.Tags.AnyAsync(x => x.UserId == userId && x.Name == name))
            throw new InvalidOperationException("Tag already exists.");

        var tag = new Tag
        {
            Name = name,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();

        return new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            CreatedAt = tag.CreatedAt
        };
    }

    public async Task<bool> DeleteTagAsync(int userId, int tagId)
    {
        var tag = await _db.Tags
            .Include(x => x.Notes)
            .FirstOrDefaultAsync(x => x.Id == tagId && x.UserId == userId);

        if (tag == null)
            return false;

        tag.Notes.Clear();
        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<List<Tag>> ResolveTagsAsync(int userId, List<int> tagIds)
    {
        var distinctIds = tagIds.Distinct().ToList();
        if (distinctIds.Count == 0)
            return [];

        var tags = await _db.Tags
            .Where(x => x.UserId == userId && distinctIds.Contains(x.Id))
            .ToListAsync();

        if (tags.Count != distinctIds.Count)
            throw new InvalidOperationException("One or more tags were not found.");

        return tags;
    }

    private static async Task<PagedNotesResponse> ToPagedResponseAsync(
        IQueryable<Note> notes,
        NoteQuery query,
        bool trashMode = false)
    {
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            notes = notes.Where(x =>
                x.Title.Contains(term) ||
                x.Content.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tagName = query.Tag.Trim();
            notes = notes.Where(x => x.Tags.Any(t => t.Name == tagName));
        }

        if (query.Pinned.HasValue && !trashMode)
            notes = notes.Where(x => x.IsPinned == query.Pinned.Value);

        var totalCount = await notes.CountAsync();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        IQueryable<Note> ordered = trashMode
            ? notes.OrderByDescending(x => x.DeletedAt)
            : notes.OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.UpdatedAt);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedNotesResponse
        {
            Items = items.Select(MapNote).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static NoteResponse MapNote(Note note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            IsPinned = note.IsPinned,
            IsDeleted = note.IsDeleted,
            DeletedAt = note.DeletedAt,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            Tags = note.Tags
                .OrderBy(x => x.Name)
                .Select(x => new TagResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };
    }
}
