import { apiRequest } from './client'
import type {
  NoteQuery,
  NoteResponse,
  PagedNotesResponse,
} from '../types/api'

function toQueryString(query: NoteQuery = {}): string {
  const params = new URLSearchParams()
  if (query.q) params.set('q', query.q)
  if (query.tag) params.set('tag', query.tag)
  if (query.pinned !== undefined) params.set('pinned', String(query.pinned))
  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('pageSize', String(query.pageSize))
  const value = params.toString()
  return value ? `?${value}` : ''
}

export function getNotes(query?: NoteQuery): Promise<PagedNotesResponse> {
  return apiRequest<PagedNotesResponse>(`/api/notes${toQueryString(query)}`)
}

export function getTrash(query?: NoteQuery): Promise<PagedNotesResponse> {
  return apiRequest<PagedNotesResponse>(
    `/api/notes/trash${toQueryString(query)}`,
  )
}

export function getNote(id: number): Promise<NoteResponse> {
  return apiRequest<NoteResponse>(`/api/notes/${id}`)
}

export function createNote(payload: {
  title: string
  content: string
  isPinned?: boolean
  tagIds?: number[]
}): Promise<NoteResponse> {
  return apiRequest<NoteResponse>('/api/notes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateNote(
  id: number,
  payload: {
    title: string
    content: string
    isPinned?: boolean
    tagIds?: number[]
  },
): Promise<NoteResponse> {
  return apiRequest<NoteResponse>(`/api/notes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteNote(id: number): Promise<void> {
  return apiRequest<void>(`/api/notes/${id}`, { method: 'DELETE' })
}

export function restoreNote(id: number): Promise<NoteResponse> {
  return apiRequest<NoteResponse>(`/api/notes/${id}/restore`, {
    method: 'POST',
  })
}

export function togglePin(id: number): Promise<NoteResponse> {
  return apiRequest<NoteResponse>(`/api/notes/${id}/pin`, {
    method: 'PATCH',
  })
}
