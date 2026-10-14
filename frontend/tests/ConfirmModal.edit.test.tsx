import { fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ConfirmModal from '../src/components/ConfirmModal'
import type { Category, Proposal } from '../src/api/client'
import { CATEGORIES } from './helpers'

const SALUD = '00000000-0000-0000-0000-000000000004'

const DENTISTA: Proposal = {
  event: { title: 'Dentista', date: '2026-10-20', time: '16:00' },
  expense: { amount: 25000, categoryId: SALUD, date: '2026-10-20' },
  warnings: [],
}

function setup(options: { onCreateCategory?: (name: string) => Promise<Category> } = {}) {
  const onConfirm = vi.fn()
  const onCancel = vi.fn()
  render(
    <ConfirmModal
      proposal={DENTISTA}
      categories={CATEGORIES}
      onConfirm={onConfirm}
      onCancel={onCancel}
      onCreateCategory={options.onCreateCategory}
    />,
  )
  return { onConfirm, onCancel }
}

describe('ConfirmModal: edición y validación (US4)', () => {
  it('el evento es editable: título y fecha', async () => {
    const { onConfirm } = setup()
    const evento = within(screen.getByRole('group', { name: 'Evento' }))

    const titulo = evento.getByLabelText('Título')
    await userEvent.clear(titulo)
    await userEvent.type(titulo, 'Ortodoncia')
    fireEvent.change(evento.getByLabelText('Fecha'), { target: { value: '2026-10-22' } })
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(onConfirm).toHaveBeenCalledWith(
      expect.objectContaining({ event: { title: 'Ortodoncia', date: '2026-10-22', time: '16:00' } }),
    )
  })

  it('Escape cancela sin confirmar nada', () => {
    const { onConfirm, onCancel } = setup()

    fireEvent(screen.getByRole('dialog'), new Event('cancel', { cancelable: true }))

    expect(onCancel).toHaveBeenCalledTimes(1)
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it.each([['', 'vacío'], ['-5', 'negativo'], ['0', 'cero']])('un monto %s (%s) bloquea "Confirmar"', async (value) => {
    const { onConfirm } = setup()
    const monto = within(screen.getByRole('group', { name: 'Gasto' })).getByLabelText('Monto')

    await userEvent.clear(monto)
    if (value) await userEvent.type(monto, value)
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Ingresá un monto mayor a 0.')
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('un título vacío bloquea "Confirmar"', async () => {
    const { onConfirm } = setup()

    await userEvent.clear(within(screen.getByRole('group', { name: 'Evento' })).getByLabelText('Título'))
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Ingresá un título.')
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('una fecha vacía bloquea "Confirmar", en el evento y en el gasto', async () => {
    const { onConfirm } = setup()

    fireEvent.change(within(screen.getByRole('group', { name: 'Evento' })).getByLabelText('Fecha'), { target: { value: '' } })
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Ingresá una fecha válida.')

    fireEvent.change(within(screen.getByRole('group', { name: 'Evento' })).getByLabelText('Fecha'), { target: { value: '2026-10-20' } })
    fireEvent.change(within(screen.getByRole('group', { name: 'Gasto' })).getByLabelText('Fecha'), { target: { value: '' } })
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Ingresá una fecha válida.')
    expect(onConfirm).not.toHaveBeenCalled()
  })

  describe('categorías propias (FR-004b)', () => {
    it('se puede agregar una categoría desde el modal y queda seleccionada', async () => {
      const onCreateCategory = vi.fn().mockResolvedValue({ id: 'c-mascotas', name: 'Mascotas', isSystem: false })
      const { onConfirm } = setup({ onCreateCategory })

      await userEvent.click(screen.getByRole('button', { name: 'Agregar categoría' }))
      await userEvent.type(screen.getByLabelText('Nueva categoría'), 'Mascotas')
      await userEvent.click(screen.getByRole('button', { name: 'Guardar categoría' }))

      expect(onCreateCategory).toHaveBeenCalledWith('Mascotas')
      expect(await screen.findByRole('option', { name: 'Mascotas' })).toBeInTheDocument()
      expect(within(screen.getByRole('group', { name: 'Gasto' })).getByLabelText('Categoría')).toHaveDisplayValue('Mascotas')

      await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
      expect(onConfirm).toHaveBeenCalledWith(
        expect.objectContaining({ expense: expect.objectContaining({ categoryId: 'c-mascotas' }) }),
      )
    })

    it('si el nombre ya existe muestra el error y no cambia la categoría', async () => {
      const onCreateCategory = vi.fn().mockRejectedValue(new Error('ya existe'))
      setup({ onCreateCategory })

      await userEvent.click(screen.getByRole('button', { name: 'Agregar categoría' }))
      await userEvent.type(screen.getByLabelText('Nueva categoría'), 'Comida')
      await userEvent.click(screen.getByRole('button', { name: 'Guardar categoría' }))

      expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo crear la categoría')
      expect(within(screen.getByRole('group', { name: 'Gasto' })).getByLabelText('Categoría')).toHaveDisplayValue('Salud')
    })

    it('sin la función de alta no ofrece agregar categorías', () => {
      setup()

      expect(screen.queryByRole('button', { name: 'Agregar categoría' })).not.toBeInTheDocument()
    })
  })
})
