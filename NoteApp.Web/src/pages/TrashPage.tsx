import { useEffect, useState } from 'react'
import * as notesApi from '../api/notesApi'
import type { NoteResponse } from '../types/api'

export function TrashPage() {
  const [notes, setNotes] = useState<NoteResponse[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  async function load() {
    setLoading(true)
    setError('')
    try {
      const page = await notesApi.getTrash({ page: 1, pageSize: 50 })
      setNotes(page.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trash')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function onRestore(id: number) {
    try {
      await notesApi.restoreNote(id)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Restore failed')
    }
  }

  return (
    <div className="stack">
      <div>
        <h1>Trash</h1>
        <p className="muted">Soft-deleted notes can be restored.</p>
      </div>

      {error ? <div className="error">{error}</div> : null}
      {loading ? <p className="muted">Loading…</p> : null}

      <div className="note-list">
        {notes.map((note) => (
          <article key={note.id} className="note-item">
            <div className="row between">
              <div>
                <h2>{note.title}</h2>
                <p className="muted">
                  Deleted{' '}
                  {note.deletedAt
                    ? new Date(note.deletedAt).toLocaleString()
                    : '—'}
                </p>
              </div>
              <button
                type="button"
                className="btn primary"
                onClick={() => void onRestore(note.id)}
              >
                Restore
              </button>
            </div>
            <p className="preview">{note.content || 'No content'}</p>
          </article>
        ))}
        {!loading && notes.length === 0 ? (
          <p className="muted">Trash is empty.</p>
        ) : null}
      </div>
    </div>
  )
}
