import { readFileSync } from 'node:fs'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ConfirmModal from '../src/components/ConfirmModal'
import EntryInput from '../src/components/EntryInput'
import { CAFE_PROPOSAL, CATEGORIES } from './helpers'

describe('accesibilidad del modal (FR-015)', () => {
  it('es un diálogo modal con nombre accesible y se abre con showModal', () => {
    const showModal = vi.spyOn(HTMLDialogElement.prototype, 'showModal')
    render(<ConfirmModal proposal={CAFE_PROPOSAL} categories={CATEGORIES} onConfirm={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByRole('dialog', { name: '¿Está todo bien?' })).toBeInTheDocument()
    expect(showModal).toHaveBeenCalledTimes(1)
  })

  it('todos los controles se alcanzan solo con el teclado, en orden, hasta Confirmar y Cancelar', async () => {
    render(<ConfirmModal proposal={CAFE_PROPOSAL} categories={CATEGORIES} onConfirm={vi.fn()} onCancel={vi.fn()} />)

    const visited: string[] = []
    for (let i = 0; i < 5; i++) {
      await userEvent.tab()
      visited.push(document.activeElement?.getAttribute('type') ?? document.activeElement?.tagName.toLowerCase() ?? '')
    }

    expect(visited).toEqual(['number', 'select', 'date', 'submit', 'button'])
  })

  it('cada campo tiene una etiqueta asociada', () => {
    render(<ConfirmModal proposal={CAFE_PROPOSAL} categories={CATEGORIES} onConfirm={vi.fn()} onCancel={vi.fn()} />)

    for (const label of ['Monto', 'Categoría', 'Fecha']) {
      expect(screen.getByLabelText(label)).toBeInTheDocument()
    }
  })

  it('el campo de texto principal tiene etiqueta y se maneja con teclado', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<EntryInput onSubmit={onSubmit} />)

    await userEvent.tab()
    await userEvent.keyboard('café 2500{Enter}')

    expect(onSubmit).toHaveBeenCalledWith('café 2500')
  })
})

// jsdom no calcula el layout: el ancho real a 360px se comprueba a mano (quickstart, escenario 12). Acá se
// fijan las reglas de CSS que lo hacen posible: nada de anchos fijos mayores y el contenido nunca desborda.
describe('reglas de CSS para 360px (RNF-02)', () => {
  const css = readFileSync('src/styles.css', 'utf-8')

  it('usa box-sizing border-box y limita el ancho al de la pantalla', () => {
    expect(css).toMatch(/box-sizing:\s*border-box/)
    expect(css).toMatch(/max-width:\s*100%/)
  })

  it('no declara anchos fijos en píxeles mayores a 360', () => {
    const widths = [...css.matchAll(/(?<![-a-z])width:\s*(\d+)px/g)].map((m) => Number(m[1]))

    expect(widths.filter((w) => w > 360)).toEqual([])
  })

  it('evita el scroll horizontal del documento', () => {
    expect(css).toMatch(/overflow-x:\s*hidden/)
  })
})
