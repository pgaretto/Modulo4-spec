import { ApiError, confirmEntry, createProposal, userMessage } from '../src/api/client'

function mockFetch(status: number, body?: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
  localStorage.clear()
})

describe('cliente de la API', () => {
  it('envía el texto y la fecha de hoy, y el token si existe', async () => {
    localStorage.setItem('token', 'abc')
    const fetchMock = mockFetch(200, { event: null, expense: null, warnings: [] })

    await createProposal('café 2500', '2026-10-14')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toMatch(/\/api\/proposals$/)
    expect(JSON.parse(init.body)).toEqual({ text: 'café 2500', today: '2026-10-14' })
    expect(init.headers.Authorization).toBe('Bearer abc')
  })

  it.each([
    [401, 'unauthorized'],
    [422, 'nothing-recognized'],
    [504, 'ai-unavailable'],
    [400, 'validation'],
    [500, 'server'],
  ])('mapea el estado %i al tipo %s', async (status, kind) => {
    mockFetch(status, { code: 'X', message: 'detalle' })

    await expect(confirmEntry({ event: null, expense: null })).rejects.toMatchObject({
      kind,
      status,
    })
  })

  it('distingue la falta de conexión de un error del servidor', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const error = await createProposal('x', '2026-10-14').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ kind: 'network', status: null })
  })

  it('da un mensaje claro para sesión vencida y texto no reconocido', () => {
    expect(userMessage(new ApiError('unauthorized', 401, ''))).toBe(
      'Tu sesión venció, iniciá sesión de nuevo.',
    )
    expect(userMessage(new ApiError('nothing-recognized', 422, ''))).toMatch(/ningún evento ni gasto/)
  })
})
