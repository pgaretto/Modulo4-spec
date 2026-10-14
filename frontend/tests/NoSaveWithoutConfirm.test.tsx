import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../src/App'
import { CAFE_PROPOSAL, CATEGORIES, mockApi } from './helpers'

const LABEL = '¿Qué querés anotar?'

function routes() {
  return mockApi({
    'GET /api/categories': { body: CATEGORIES },
    'POST /api/proposals': { body: CAFE_PROPOSAL },
    'POST /api/entries': { status: 201, body: { event: null, expense: { id: 'x', ...CAFE_PROPOSAL.expense } } },
  })
}

async function sendText() {
  await userEvent.type(screen.getByLabelText(LABEL), 'Me tomé un café 2500')
  await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))
  return screen.findByRole('dialog', { name: '¿Está todo bien?' })
}

afterEach(() => vi.unstubAllGlobals())

// Principio I / SC-004: ninguna acción de la interfaz guarda sin el "Confirmar" del modal.
describe('nada se guarda sin confirmar', () => {
  it('al enviar el texto solo se pide la propuesta; no se guarda nada', async () => {
    const api = routes()
    render(<App />)

    await sendText()

    expect(api.calls.some((c) => c.path === '/api/proposals')).toBe(true)
    expect(api.entries()).toHaveLength(0)
  })

  it('cancelar con el botón o con Escape no guarda nada', async () => {
    const api = routes()
    render(<App />)

    const dialog = await sendText()
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())

    const second = await sendText()
    fireEvent(second, new Event('cancel', { cancelable: true }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())

    expect(dialog).toBeDefined()
    expect(api.entries()).toHaveLength(0)
  })

  it('solo "Confirmar" guarda, con los datos del modal, y deja el campo vacío', async () => {
    const api = routes()
    render(<App />)

    await sendText()
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(api.entries()).toHaveLength(1)
    expect(api.entries()[0].body).toEqual({
      event: null,
      expense: { amount: 2500, categoryId: CAFE_PROPOSAL.expense!.categoryId, date: '2026-10-14' },
    })
    expect(screen.getByLabelText(LABEL)).toHaveValue('')
  })
})
