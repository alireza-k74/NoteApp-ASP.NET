import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import * as notesApi from '../api/notesApi'
import * as tagsApi from '../api/tagsApi'
import type { NoteResponse, TagResponse } from '../types/api'

export function NotesPage() {
  const [notes, setNotes] = useState<NoteResponse[]>([])
  const [tags, setTags] = useState<TagResponse[]>([])
  const [q, setQ] = useState('')
  const [tag, setTag] = useState('')
  const [pinnedOnly, setPinnedOnly] = useState(false)
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [pageSize] = useState(10)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  async function load(pageOverride?: number) {
    setLoading(true)
    setError('')
    try {
      const [notesPage, tagList] = await Promise.all([
        notesApi.getNotes({
          q: q || undefined,
          tag: tag || undefined,
          pinned: pinnedOnly ? true : undefined,
          page: pageOverride ?? page,
          pageSize,
        }),
        tagsApi.getTags(),
      ])
      setNotes(notesPage.items)
      setTotalCount(notesPage.totalCount)
      setTags(tagList)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load notes')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, tag, pinnedOnly])

  async function onSearch(event: FormEvent) {
    event.preventDefault()
    setPage(1)
    await load(1)
  }

  async function onTogglePin(id: number) {
    try {
      await notesApi.togglePin(id)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Pin failed')
    }
  }

  async function onDelete(id: number) {
    try {
      await notesApi.deleteNote(id)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed')
    }
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <div className="stack">
      <div className="row between">
        <div>
          <h1>Notes</h1>
          <p className="muted">{totalCount} note(s)</p>
        </div>
        <Link className="btn primary" to="/notes/new">
          New note
        </Link>
      </div>

      <form className="filters" onSubmit={onSearch}>
        <input
          placeholder="Search title or content"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <select value={tag} onChange={(e) => setTag(e.target.value)}>
          <option value="">All tags</option>
          {tags.map((item) => (
            <option key={item.id} value={item.name}>
              {item.name}
            </option>
          ))}
        </select>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={pinnedOnly}
            onChange={(e) => {
              setPage(1)
              setPinnedOnly(e.target.checked)
            }}
          />
          Pinned only
        </label>
        <button className="btn" type="submit">
          Search
        </button>
      </form>

      {error ? <div className="error">{error}</div> : null}
      {loading ? <p className="muted">Loading…</p> : null}

      <div className="note-list">
        {notes.map((note) => (
          <article key={note.id} className={`note-item ${note.isPinned ? 'pinned' : ''}`}>
            <div className="row between">
              <Link to={`/notes/${note.id}`}>
                <h2>{note.title}</h2>
              </Link>
              <div className="row gap">
                <button
                  type="button"
                  className="btn ghost"
                  onClick={() => void onTogglePin(note.id)}
                >
                  {note.isPinned ? 'Unpin' : 'Pin'}
                </button>
                <button
                  type="button"
                  className="btn danger"
                  onClick={() => void onDelete(note.id)}
                >
                  Delete
                </button>
              </div>
            </div>
            <p className="preview">{note.content || 'No content'}</p>
            <div className="tag-row">
              {note.tags.map((item) => (
                <span key={item.id} className="tag">
                  {item.name}
                </span>
              ))}
            </div>
          </article>
        ))}
        {!loading && notes.length === 0 ? (
          <p className="muted">No notes found.</p>
        ) : null}
      </div>

      <div className="row gap">
        <button
          type="button"
          className="btn"
          disabled={page <= 1}
          onClick={() => setPage((p) => p - 1)}
        >
          Previous
        </button>
        <span className="muted">
          Page {page} / {totalPages}
        </span>
        <button
          type="button"
          className="btn"
          disabled={page >= totalPages}
          onClick={() => setPage((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}
