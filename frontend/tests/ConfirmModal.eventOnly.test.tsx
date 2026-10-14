import { render, screen } from '@testing-library/react'
import ConfirmModal from '../src/components/ConfirmModal'
import type { Proposal } from '../src/api/client'
import { CATEGORIES } from './helpers'

const REUNION: Proposal = {
  event: { title: 'Reunión con mamá', date: '2026-10-15', time: '10:00' },
  expense: null,
  warnings: [],
}

describe('ConfirmModal con solo un evento (US3, AC-04)', () => {
  it('muestra el evento y no ofrece ninguna sección de gasto', () => {
    render(<ConfirmModal proposal={REUNION} categories={CATEGORIES} onConfirm={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByRole('group', { name: 'Evento' })).toBeInTheDocument()
    expect(screen.getByLabelText('Título')).toHaveValue('Reunión con mamá')
    expect(screen.queryByRole('group', { name: 'Gasto' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Monto')).not.toBeInTheDocument()
  })
})
