import type { Category, Proposal } from '../src/api/client'

export const COMIDA = '00000000-0000-0000-0000-000000000001'
export const TRANSPORTE = '00000000-0000-0000-0000-000000000002'

export const CATEGORIES: Category[] = [
  { id: COMIDA, name: 'Comida', isSystem: true },
  { id: TRANSPORTE, name: 'Transporte', isSystem: true },
  { id: '00000000-0000-0000-0000-000000000003', name: 'Salidas', isSystem: true },
  { id: '00000000-0000-0000-0000-000000000004', name: 'Salud', isSystem: true },
  { id: '00000000-0000-0000-0000-000000000005', name: 'Otros', isSystem: true },
]

export const CAFE_PROPOSAL: Proposal = {
  event: null,
  expense: { amount: 2500, categoryId: COMIDA, date: '2026-10-14' },
  warnings: [],
}

interface Route {
  status?: number
  body?: unknown
}

/** Simula la API por ruta y método, y registra cada llamada para poder verificarla. */
export function mockApi(routes: Record<string, Route>) {
  const calls: { method: string; path: string; body: unknown }[] = []
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    const path = new URL(url).pathname
    const method = init?.method ?? 'GET'
    calls.push({ method, path, body: init?.body ? JSON.parse(init.body as string) : undefined })
    const route = routes[`${method} ${path}`] ?? { status: 404, body: { code: 'NOT_FOUND', message: 'x' } }
    return new Response(route.body === undefined ? null : JSON.stringify(route.body), {
      status: route.status ?? 200,
      headers: { 'Content-Type': 'application/json' },
    })
  })
  vi.stubGlobal('fetch', fetchMock)
  return { calls, entries: () => calls.filter((c) => c.path === '/api/entries') }
}
