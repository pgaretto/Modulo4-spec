import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../src/App'
import { mockApi } from './helpers'

const SALUD = '00000000-0000-0000-0000-000000000004'
const EVENTO = { id: 'ev-1', title: 'Dentista', date: '2026-10-20', time: '16:00' }
const GASTO = { id: 'ex-1', amount: 25000, categoryId: SALUD, date: '2026-10-20' }

function setup() {
  return mockApi({
    'GET /api/events': { body: [EVENTO] },
    'GET /api/expenses': { body: [GASTO] },
    'PUT /api/expenses/ex-1': { body: { ...GASTO, amount: 30000 } },
    'PUT /api/events/ev-1': { body: { ...EVENTO, title: 'Ortodoncia' } },
    'DELETE /api/events/ev-1': { status: 204 },
  })
}

const lists = (api: ReturnType<typeof setup>) => api.calls.filter((c) => c.method === 'GET' && c.path === '/api/events').length
const writes = (api: ReturnType<typeof setup>, method: string) => api.calls.filter((c) => c.method === method)

afterEach(() => vi.unstubAllGlobals())

describe('lista de ítems guardados del mes (FR-007e)', () => {
  it('muestra los eventos y gastos del mes', async () => {
    setup()
    render(<App />)

    const lista = await screen.findByRole('region', { name: 'Ítems del mes' })
    expect(within(lista).getByText(/Dentista/)).toBeInTheDocument()
    expect(within(lista).getByText(/\$25\.000/)).toBeInTheDocument()
    expect(within(lista).getByText(/Salud/)).toBeInTheDocument()
  })

  it('sin ítems muestra un mensaje en vez de una lista vacía', async () => {
    mockApi({})
    render(<App />)

    expect(await screen.findByText('Todavía no hay eventos ni gastos este mes.')).toBeInTheDocument()
  })
})

describe('editar un ítem guardado (FR-007b)', () => {
  it('abre el mismo modal con los datos cargados y guarda recién al confirmar', async () => {
    const api = setup()
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: /Editar gasto de \$25\.000/ }))

    const dialog = await screen.findByRole('dialog', { name: '¿Está todo bien?' })
    const monto = within(dialog).getByLabelText('Monto')
    expect(monto).toHaveValue(25000)
    expect(within(dialog).getByLabelText('Categoría')).toHaveDisplayValue('Salud')
    expect(writes(api, 'PUT')).toHaveLength(0)

    await userEvent.clear(monto)
    await userEvent.type(monto, '30000')
    expect(writes(api, 'PUT')).toHaveLength(0)
    await userEvent.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(writes(api, 'PUT')).toHaveLength(1))
    expect(writes(api, 'PUT')[0]).toMatchObject({
      path: '/api/expenses/ex-1',
      body: { amount: 30000, categoryId: SALUD, date: '2026-10-20' },
    })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(lists(api)).toBeGreaterThan(1) // la lista se vuelve a cargar con el cambio
  })

  it('edita un evento desde el mismo modal', async () => {
    const api = setup()
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Editar Dentista' }))
    const dialog = await screen.findByRole('dialog', { name: '¿Está todo bien?' })
    const titulo = within(dialog).getByLabelText('Título')
    await userEvent.clear(titulo)
    await userEvent.type(titulo, 'Ortodoncia')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(writes(api, 'PUT')).toHaveLength(1))
    expect(writes(api, 'PUT')[0]).toMatchObject({
      path: '/api/events/ev-1',
      body: { title: 'Ortodoncia', date: '2026-10-20', time: '16:00' },
    })
  })

  it('cancelar la edición no cambia nada', async () => {
    const api = setup()
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: /Editar gasto de/ }))
    await userEvent.click(await screen.findByRole('button', { name: 'Cancelar' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(writes(api, 'PUT')).toHaveLength(0)
  })
})

describe('borrar un ítem guardado (FR-007c)', () => {
  it('pide confirmación y, si se rechaza, el ítem sigue guardado', async () => {
    const api = setup()
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Borrar Dentista' }))
    const confirmacion = await screen.findByRole('dialog', { name: /¿Borrar/ })
    expect(confirmacion).toHaveTextContent('No se puede deshacer')
    expect(writes(api, 'DELETE')).toHaveLength(0)

    await userEvent.click(within(confirmacion).getByRole('button', { name: 'Cancelar' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(writes(api, 'DELETE')).toHaveLength(0)
    expect(screen.getByText(/Dentista/)).toBeInTheDocument()
  })

  it('al aceptar la confirmación borra el ítem y recarga la lista', async () => {
    const api = setup()
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Borrar Dentista' }))
    await userEvent.click(within(await screen.findByRole('dialog', { name: /¿Borrar/ })).getByRole('button', { name: 'Sí, borrar' }))

    await waitFor(() => expect(writes(api, 'DELETE')).toHaveLength(1))
    expect(writes(api, 'DELETE')[0].path).toBe('/api/events/ev-1')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(lists(api)).toBeGreaterThan(1)
  })
})
