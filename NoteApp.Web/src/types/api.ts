export type TokenResponse = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export type UserResponse = {
  id: number
  username: string
  email: string
}

export type TagResponse = {
  id: number
  name: string
  createdAt: string
}

export type NoteResponse = {
  id: number
  title: string
  content: string
  isPinned: boolean
  isDeleted: boolean
  deletedAt: string | null
  createdAt: string
  updatedAt: string
  tags: TagResponse[]
}

export type PagedNotesResponse = {
  items: NoteResponse[]
  totalCount: number
  page: number
  pageSize: number
}

export type NoteQuery = {
  q?: string
  tag?: string
  pinned?: boolean
  page?: number
  pageSize?: number
}
