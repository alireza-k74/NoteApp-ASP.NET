import { apiRequest } from './client'
import type { TagResponse } from '../types/api'

export function getTags(): Promise<TagResponse[]> {
  return apiRequest<TagResponse[]>('/api/tags')
}

export function createTag(name: string): Promise<TagResponse> {
  return apiRequest<TagResponse>('/api/tags', {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

export function deleteTag(id: number): Promise<void> {
  return apiRequest<void>(`/api/tags/${id}`, { method: 'DELETE' })
}
