using NoteApp.Api.DTOs;

namespace NoteApp.Api.Services;

public interface INoteService
{
    Task<PagedNotesResponse> GetNotesAsync(int userId, NoteQuery query);

    Task<PagedNotesResponse> GetTrashAsync(int userId, NoteQuery query);

    Task<NoteResponse?> GetNoteAsync(int userId, int noteId);

    Task<NoteResponse> CreateNoteAsync(int userId, CreateNoteRequest request);

    Task<NoteResponse?> UpdateNoteAsync(int userId, int noteId, UpdateNoteRequest request);

    Task<bool> SoftDeleteAsync(int userId, int noteId);

    Task<NoteResponse?> RestoreAsync(int userId, int noteId);

    Task<NoteResponse?> TogglePinAsync(int userId, int noteId);

    Task<List<TagResponse>> GetTagsAsync(int userId);

    Task<TagResponse> CreateTagAsync(int userId, CreateTagRequest request);

    Task<bool> DeleteTagAsync(int userId, int tagId);
}
