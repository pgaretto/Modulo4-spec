import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../src/App'
import { CATEGORIES, mockApi } from './helpers'

const LABEL = '¿Qué querés anotar?'

afterEach(() => vi.unstubAllGlobals())

describe('manejo de errores del flujo principal', () => {
  it('con la sesión vencida avisa, conserva el texto y señala el login (FR-012)', async () => {
    mockApi({
      'GET /api/categories': { body: CATEGORIES },
      'POST /api/proposals': { status: 401, body: { code: 'UNAUTHORIZED', message: 'x' } },
    })
    const onUnauthorized = vi.fn()
    render(<App onUnauthorized={onUnauthorized} />)

    await userEvent.type(screen.getByLabelText(LABEL), 'café 2500')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Tu sesión venció, iniciá sesión de nuevo')
    expect(screen.getByLabelText(LABEL)).toHaveValue('café 2500')
    expect(onUnauthorized).toHaveBeenCalledTimes(1)
  })

  it('si la IA no responde informa el error y conserva el texto (FR-011)', async () => {
    mockApi({
      'GET /api/categories': { body: CATEGORIES },
      'POST /api/proposals': { status: 504, body: { code: 'AI_UNAVAILABLE', message: 'x' } },
    })
    render(<App />)

    await userEvent.type(screen.getByLabelText(LABEL), 'café 2500')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo interpretar el texto')
    expect(screen.getByLabelText(LABEL)).toHaveValue('café 2500')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('si no hay evento ni gasto avisa y no abre el modal', async () => {
    mockApi({
      'GET /api/categories': { body: CATEGORIES },
      'POST /api/proposals': { status: 422, body: { code: 'NOTHING_RECOGNIZED', message: 'x' } },
    })
    render(<App />)

    await userEvent.type(screen.getByLabelText(LABEL), 'hola, ¿cómo andás?')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No encontré ningún evento ni gasto')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
