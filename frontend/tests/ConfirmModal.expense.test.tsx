import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ConfirmModal from '../src/components/ConfirmModal'
import { CAFE_PROPOSAL, CATEGORIES, COMIDA, TRANSPORTE } from './helpers'

function setup() {
  const onConfirm = vi.fn()
  const onCancel = vi.fn()
  render(
    <ConfirmModal proposal={CAFE_PROPOSAL} categories={CATEGORIES} onConfirm={onConfirm} onCancel={onCancel} />,
  )
  return { onConfirm, onCancel }
}

describe('ConfirmModal con un gasto propuesto', () => {
  it('muestra "¿Está todo bien?" con el gasto precompletado (AC-05)', () => {
    setup()

    expect(screen.getByRole('dialog', { name: '¿Está todo bien?' })).toBeInTheDocument()
    expect(screen.getByLabelText('Monto')).toHaveValue(2500)
    expect(screen.getByLabelText('Categoría')).toHaveDisplayValue('Comida')
    expect(screen.getByLabelText('Fecha')).toHaveValue('2026-10-14')
  })

  it('"Confirmar" entrega el gasto tal como está, sin evento', async () => {
    const { onConfirm } = setup()

    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(onConfirm).toHaveBeenCalledWith({
      event: null,
      expense: { amount: 2500, categoryId: COMIDA, date: '2026-10-14' },
    })
  })

  it('los campos son editables y se entrega el valor corregido', async () => {
    const { onConfirm } = setup()

    const amount = screen.getByLabelText('Monto')
    await userEvent.clear(amount)
    await userEvent.type(amount, '3000')
    await userEvent.selectOptions(screen.getByLabelText('Categoría'), 'Transporte')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(onConfirm).toHaveBeenCalledWith({
      event: null,
      expense: { amount: 3000, categoryId: TRANSPORTE, date: '2026-10-14' },
    })
  })

  it('"Cancelar" no confirma nada', async () => {
    const { onConfirm, onCancel } = setup()

    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(onCancel).toHaveBeenCalledTimes(1)
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('un monto vacío o no positivo bloquea la confirmación', async () => {
    const { onConfirm } = setup()
    const amount = screen.getByLabelText('Monto')

    await userEvent.clear(amount)
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Ingresá un monto mayor a 0.')

    await userEvent.type(amount, '-5')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(onConfirm).not.toHaveBeenCalled()
  })
})
