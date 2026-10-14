// Cliente HTTP tipado según specs/001-texto-ia-confirmacion/contracts/openapi.yaml.

export type ProposalWarning =
  | 'MULTIPLE_EVENTS'
  | 'MULTIPLE_EXPENSES'
  | 'DEFAULT_CATEGORY'
  | 'AMBIGUOUS_DATE'

/** `time` en formato HH:mm; null = todo el día. */
export interface EventInput {
  title: string
  date: string
  time: string | null
}

export interface ExpenseInput {
  amount: number
  categoryId: string
  date: string
}

export interface Proposal {
  event: EventInput | null
  expense: ExpenseInput | null
  warnings: ProposalWarning[]
}

export interface ConfirmRequest {
  event: EventInput | null
  expense: ExpenseInput | null
}

export interface SavedEvent extends EventInput {
  id: string
}

export interface SavedExpense extends ExpenseInput {
  id: string
}

export interface ConfirmResponse {
  event: SavedEvent | null
  expense: SavedExpense | null
}

export interface Category {
  id: string
  name: string
  isSystem: boolean
}

export type ApiErrorKind =
  | 'unauthorized' // 401: sin sesión o sesión vencida (FR-012)
  | 'nothing-recognized' // 422: el texto no trae evento ni gasto
  | 'ai-unavailable' // 504: la IA no respondió a tiempo o falló (FR-011)
  | 'validation' // 400
  | 'not-found' // 404
  | 'conflict' // 409
  | 'network' // sin conexión
  | 'server'

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status: number | null

  constructor(kind: ApiErrorKind, status: number | null, message: string) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
  }
}

const API_BASE_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5190'

/** Token de desarrollo (`dotnet user-jwts create`) hasta que exista la pantalla de login. */
function getToken(): string | null {
  const fromEnv: string | undefined = import.meta.env.VITE_DEV_TOKEN
  if (fromEnv) return fromEnv
  try {
    return localStorage.getItem('token')
  } catch {
    return null
  }
}

const KIND_BY_STATUS: Record<number, ApiErrorKind> = {
  400: 'validation',
  401: 'unauthorized',
  404: 'not-found',
  409: 'conflict',
  422: 'nothing-recognized',
  504: 'ai-unavailable',
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken()
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init.headers,
      },
    })
  } catch {
    throw new ApiError('network', null, 'Sin conexión con el servidor.')
  }

  if (!response.ok) {
    let message = `Error ${response.status}`
    try {
      const body = (await response.json()) as { message?: string }
      if (body.message) message = body.message
    } catch {
      // El cuerpo no es JSON; se conserva el mensaje genérico.
    }
    throw new ApiError(KIND_BY_STATUS[response.status] ?? 'server', response.status, message)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

const post = <T>(path: string, body: unknown) =>
  request<T>(path, { method: 'POST', body: JSON.stringify(body) })

/** Interpreta la frase y devuelve una propuesta. No guarda nada (Principio I). */
export const createProposal = (text: string, today: string) =>
  post<Proposal>('/api/proposals', { text, today })

/** Guarda lo ya confirmado por el usuario en el modal. Es la única vía de alta. */
export const confirmEntry = (entry: ConfirmRequest) => post<ConfirmResponse>('/api/entries', entry)

export const listCategories = () => request<Category[]>('/api/categories')

/** Mensaje para mostrarle al usuario según el tipo de error. */
export function userMessage(error: unknown): string {
  if (!(error instanceof ApiError)) return 'Ocurrió un error inesperado. Probá de nuevo.'
  switch (error.kind) {
    case 'unauthorized':
      return 'Tu sesión venció, iniciá sesión de nuevo.'
    case 'nothing-recognized':
      return 'No encontré ningún evento ni gasto en esa frase. Probá con más detalle.'
    case 'ai-unavailable':
      return 'No se pudo interpretar el texto. Probá de nuevo.'
    case 'network':
      return 'Sin conexión con el servidor. Probá de nuevo.'
    case 'validation':
      return error.message
    default:
      return 'Ocurrió un error inesperado. Probá de nuevo.'
  }
}
