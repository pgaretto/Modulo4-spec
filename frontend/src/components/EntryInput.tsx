import { useState, type FormEvent } from 'react'

const MAX_LENGTH = 500

interface EntryInputProps {
  /** Pide la propuesta. El campo conserva el texto: solo se vacía al guardar (lo decide quien lo usa con `key`). */
  onSubmit: (text: string) => Promise<void>
  error?: string | null
}

/** Campo único de texto libre para anotar un evento y/o gasto (RF-04). */
function EntryInput({ onSubmit, error }: EntryInputProps) {
  const [text, setText] = useState('')
  const [loading, setLoading] = useState(false)
  const trimmed = text.trim()

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!trimmed || loading) return

    setLoading(true)
    try {
      await onSubmit(trimmed)
    } finally {
      setLoading(false)
    }
  }

  return (
    <section aria-label="Cargar evento o gasto">
      <form onSubmit={handleSubmit}>
        <label>
          ¿Qué querés anotar?
          <input
            type="text"
            value={text}
            maxLength={MAX_LENGTH}
            placeholder="Mañana almuerzo con mamá y gasto 15000"
            disabled={loading}
            onChange={(e) => setText(e.target.value)}
          />
        </label>
        {text.length >= MAX_LENGTH && <p>Máximo {MAX_LENGTH} caracteres</p>}
        <button type="submit" disabled={loading || !trimmed}>
          {loading ? 'Interpretando…' : 'Enviar'}
        </button>
      </form>
      {error && <p role="alert">{error}</p>}
    </section>
  )
}

export default EntryInput
