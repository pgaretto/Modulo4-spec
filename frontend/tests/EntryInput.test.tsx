import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EntryInput from '../src/components/EntryInput'

const LABEL = '¿Qué querés anotar?'

describe('EntryInput', () => {
  it('no deja enviar un texto vacío ni de solo espacios', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<EntryInput onSubmit={onSubmit} />)
    const button = screen.getByRole('button', { name: 'Enviar' })

    expect(button).toBeDisabled()
    await userEvent.type(screen.getByLabelText(LABEL), '   ')

    expect(button).toBeDisabled()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('envía el texto sin espacios sobrantes', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<EntryInput onSubmit={onSubmit} />)

    await userEvent.type(screen.getByLabelText(LABEL), '  Me tomé un café 2500  ')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(onSubmit).toHaveBeenCalledWith('Me tomé un café 2500')
  })

  it('muestra el indicador de carga y no permite un segundo envío mientras procesa', async () => {
    let finish: () => void = () => {}
    const onSubmit = vi.fn().mockReturnValue(new Promise<void>((resolve) => (finish = resolve)))
    render(<EntryInput onSubmit={onSubmit} />)
    const input = screen.getByLabelText(LABEL)

    await userEvent.type(input, 'café 2500')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(screen.getByRole('button', { name: 'Interpretando…' })).toBeDisabled()
    expect(input).toBeDisabled()
    await userEvent.keyboard('{Enter}')
    expect(onSubmit).toHaveBeenCalledTimes(1)

    finish()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Enviar' })).toBeEnabled())
  })

  it('conserva el texto escrito después de enviar (solo se limpia al guardar)', async () => {
    render(<EntryInput onSubmit={vi.fn().mockResolvedValue(undefined)} />)
    const input = screen.getByLabelText(LABEL)

    await userEvent.type(input, 'café 2500')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(input).toHaveValue('café 2500')
  })

  it('avisa cuando se alcanza el máximo de 500 caracteres', async () => {
    render(<EntryInput onSubmit={vi.fn()} />)
    const input = screen.getByLabelText(LABEL)

    await userEvent.click(input)
    await userEvent.paste('a'.repeat(501))

    expect((input as HTMLInputElement).value).toHaveLength(500)
    expect(screen.getByText('Máximo 500 caracteres')).toBeInTheDocument()
  })

  it('muestra el mensaje de error que recibe', () => {
    render(<EntryInput onSubmit={vi.fn()} error="No se pudo interpretar el texto. Probá de nuevo." />)

    expect(screen.getByRole('alert')).toHaveTextContent('No se pudo interpretar el texto')
  })
})
