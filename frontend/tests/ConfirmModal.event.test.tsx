import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ConfirmModal from '../src/components/ConfirmModal'
import type { Proposal } from '../src/api/client'
import { CATEGORIES, COMIDA } from './helpers'

const SALUD = '00000000-0000-0000-0000-000000000004'

const DENTISTA: Proposal = {
  event: { title: 'Dentista', date: '2026-10-20', time: '16:00' },
  expense: { amount: 25000, categoryId: SALUD, date: '2026-10-20' },
  warnings: [],
}

function setup(proposal: Proposal) {
  const onConfirm = vi.fn()
  render(<ConfirmModal proposal={proposal} categories={CATEGORIES} onConfirm={onConfirm} onCancel={vi.fn()} />)
  return { onConfirm }
}

describe('ConfirmModal con evento y gasto (US2)', () => {
  it('muestra título, fecha, hora, monto y categoría precompletados (AC-02)', () => {
    setup(DENTISTA)

    const evento = within(screen.getByRole('group', { name: 'Evento' }))
    expect(evento.getByLabelText('Título')).toHaveValue('Dentista')
    expect(evento.getByLabelText('Fecha')).toHaveValue('2026-10-20')
    expect(evento.getByLabelText('Hora (opcional)')).toHaveValue('16:00')
    const gasto = within(screen.getByRole('group', { name: 'Gasto' }))
    expect(gasto.getByLabelText('Monto')).toHaveValue(25000)
    expect(gasto.getByLabelText('Categoría')).toHaveDisplayValue('Salud')
  })

  it('confirma evento y gasto juntos', async () => {
    const { onConfirm } = setup(DENTISTA)

    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(onConfirm).toHaveBeenCalledWith({
      event: { title: 'Dentista', date: '2026-10-20', time: '16:00' },
      expense: { amount: 25000, categoryId: SALUD, date: '2026-10-20' },
    })
  })

  it('un evento sin hora se muestra como todo el día y se confirma con hora nula', async () => {
    const { onConfirm } = setup({ ...DENTISTA, event: { title: 'Cumple de Juan', date: '2026-10-16', time: null }, expense: null })

    const evento = within(screen.getByRole('group', { name: 'Evento' }))
    expect(evento.getByLabelText('Hora (opcional)')).toHaveValue('')
    expect(evento.getByText('Sin hora: todo el día')).toBeInTheDocument()
    expect(screen.queryByRole('group', { name: 'Gasto' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(onConfirm).toHaveBeenCalledWith({
      event: { title: 'Cumple de Juan', date: '2026-10-16', time: null },
      expense: null,
    })
  })

  it('se puede agregar o cambiar la hora', async () => {
    const { onConfirm } = setup({ ...DENTISTA, expense: null })

    const hora = screen.getByLabelText('Hora (opcional)')
    await userEvent.clear(hora)
    await userEvent.type(hora, '09:30')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(onConfirm).toHaveBeenCalledWith({
      event: { title: 'Dentista', date: '2026-10-20', time: '09:30' },
      expense: null,
    })
  })

  describe('avisos (FR-014): texto exacto junto al dato afectado', () => {
    it('más de un gasto y categoría por defecto, en el gasto', () => {
      setup({
        event: null,
        expense: { amount: 100, categoryId: COMIDA, date: '2026-10-14' },
        warnings: ['MULTIPLE_EXPENSES', 'DEFAULT_CATEGORY'],
      })

      const gasto = within(screen.getByRole('group', { name: 'Gasto' }))
      expect(gasto.getByText('Había más de un gasto; se propone el primero')).toBeInTheDocument()
      expect(gasto.getByText('No pude deducir la categoría; elegí una')).toBeInTheDocument()
    })

    it('más de un evento y fecha estimada, en el evento', () => {
      setup({ ...DENTISTA, expense: null, warnings: ['MULTIPLE_EVENTS', 'AMBIGUOUS_DATE'] })

      const evento = within(screen.getByRole('group', { name: 'Evento' }))
      expect(evento.getByText('Había más de un evento; se propone el primero')).toBeInTheDocument()
      expect(evento.getByText('Fecha estimada; revisala')).toBeInTheDocument()
    })

    it('sin avisos no muestra ninguna nota', () => {
      setup(DENTISTA)

      expect(screen.queryAllByRole('note')).toHaveLength(0)
    })
  })
})
