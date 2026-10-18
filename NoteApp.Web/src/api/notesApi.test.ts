import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { getNotes } from './notesApi'

describe('notesApi.getNotes', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        text: async () =>
          JSON.stringify({
            items: [],
            totalCount: 0,
            page: 1,
            pageSize: 10,
          }),
      }),
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('builds query string for filters', async () => {
    await getNotes({ q: 'milk', tag: 'home', pinned: true, page: 2, pageSize: 5 })

    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/notes?'),
      expect.any(Object),
    )

    const url = String(vi.mocked(fetch).mock.calls[0]?.[0])
    expect(url).toContain('q=milk')
    expect(url).toContain('tag=home')
    expect(url).toContain('pinned=true')
    expect(url).toContain('page=2')
    expect(url).toContain('pageSize=5')
  })
})
