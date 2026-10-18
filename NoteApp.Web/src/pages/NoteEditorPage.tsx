import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import * as notesApi from '../api/notesApi'
import * as tagsApi from '../api/tagsApi'
import type { TagResponse } from '../types/api'

export function NoteEditorPage() {
  const { id } = useParams()
  const isNew = !id || id === 'new'
  const noteId = !isNew ? Number(id) : null
  const navigate = useNavigate()

  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [isPinned, setIsPinned] = useState(false)
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([])
  const [tags, setTags] = useState<TagResponse[]>([])
  const [newTagName, setNewTagName] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(!isNew)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    async function load() {
      setLoading(true)
      setError('')
      try {
        const tagList = await tagsApi.getTags()
        setTags(tagList)
        if (noteId) {
          const note = await notesApi.getNote(noteId)
          setTitle(note.title)
          setContent(note.content)
          setIsPinned(note.isPinned)
          setSelectedTagIds(note.tags.map((t) => t.id))
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load note')
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [noteId])

  function toggleTag(tagId: number) {
    setSelectedTagIds((current) =>
      current.includes(tagId)
        ? current.filter((x) => x !== tagId)
        : [...current, tagId],
    )
  }

  async function onCreateTag() {
    if (!newTagName.trim()) return
    try {
      const tag = await tagsApi.createTag(newTagName.trim())
      setTags((current) =>
        [...current, tag].sort((a, b) => a.name.localeCompare(b.name)),
      )
      setSelectedTagIds((current) => [...current, tag.id])
      setNewTagName('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create tag')
    }
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError('')
    const payload = {
      title: title.trim(),
      content,
      isPinned,
      tagIds: selectedTagIds,
    }
    try {
      if (noteId) {
        await notesApi.updateNote(noteId, payload)
        navigate(`/notes/${noteId}`)
      } else {
        const created = await notesApi.createNote(payload)
        navigate(`/notes/${created.id}`)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <p className="muted">Loading…</p>
  }

  return (
    <div className="stack narrow">
      <div className="row between">
        <h1>{isNew ? 'New note' : 'Edit note'}</h1>
        <Link className="btn ghost" to="/">
          Back
        </Link>
      </div>

      {error ? <div className="error">{error}</div> : null}

      <form className="stack" onSubmit={onSubmit}>
        <label>
          Title
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            maxLength={200}
          />
        </label>
        <label>
          Content
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            rows={12}
            maxLength={10000}
          />
        </label>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={isPinned}
            onChange={(e) => setIsPinned(e.target.checked)}
          />
          Pinned
        </label>

        <fieldset className="tag-picker">
          <legend>Tags</legend>
          <div className="tag-row">
            {tags.map((tag) => (
              <label key={tag.id} className="tag-choice">
                <input
                  type="checkbox"
                  checked={selectedTagIds.includes(tag.id)}
                  onChange={() => toggleTag(tag.id)}
                />
                {tag.name}
              </label>
            ))}
            {tags.length === 0 ? <span className="muted">No tags yet</span> : null}
          </div>
        </fieldset>

        <div className="row gap">
          <input
            placeholder="New tag name"
            value={newTagName}
            onChange={(e) => setNewTagName(e.target.value)}
            maxLength={50}
          />
          <button
            type="button"
            className="btn"
            onClick={() => void onCreateTag()}
          >
            Add tag
          </button>
        </div>

        <button className="btn primary" type="submit" disabled={saving}>
          {saving ? 'Saving…' : 'Save note'}
        </button>
      </form>
    </div>
  )
}
