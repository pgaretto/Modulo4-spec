import { useState } from 'react'
import {
  ApiError,
  confirmEntry,
  createCategory,
  createProposal,
  listCategories,
  userMessage,
  type Category,
  type ConfirmRequest,
  type Proposal,
} from './api/client'
import ConfirmModal from './components/ConfirmModal'
import EntryInput from './components/EntryInput'

/** Fecha local del usuario (yyyy-MM-dd): base para "mañana", "el martes que viene", etc. */
function todayLocal(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

interface AppProps {
  /** Punto de enlace con el login (otra feature): se llama cuando la sesión venció (FR-012). */
  onUnauthorized?: () => void
}

function App({ onUnauthorized }: AppProps) {
  const [proposal, setProposal] = useState<Proposal | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState<string | null>(null)
  const [modalError, setModalError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  // Cambiar la clave vuelve a montar el campo de texto vacío (solo después de guardar).
  const [entryKey, setEntryKey] = useState(0)

  const handleError = (err: unknown): string => {
    if (err instanceof ApiError && err.kind === 'unauthorized') onUnauthorized?.()
    return userMessage(err)
  }

  // Pide la propuesta a la IA. No guarda nada: eso solo ocurre al confirmar en el modal (Principio I).
  const handleSubmit = async (text: string) => {
    setError(null)
    try {
      const [result, available] = await Promise.all([
        createProposal(text, todayLocal()),
        categories.length > 0 ? Promise.resolve(categories) : listCategories(),
      ])
      setCategories(available)
      setModalError(null)
      setProposal(result)
    } catch (err) {
      setError(handleError(err))
    }
  }

  // La categoría propia se guarda al crearla, aunque después se cancele el modal.
  const handleCreateCategory = async (name: string) => {
    const category = await createCategory(name)
    setCategories((list) => [...list, category])
    return category
  }

  const handleConfirm = async (entry: ConfirmRequest) => {
    setSaving(true)
    setModalError(null)
    try {
      await confirmEntry(entry)
      setProposal(null)
      setEntryKey((key) => key + 1)
    } catch (err) {
      setModalError(handleError(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <main>
      <h1>Smart-Agenda</h1>
      <EntryInput key={entryKey} onSubmit={handleSubmit} error={error} />
      {proposal && (
        <ConfirmModal
          proposal={proposal}
          categories={categories}
          saving={saving}
          error={modalError}
          onConfirm={handleConfirm}
          onCancel={() => setProposal(null)}
          onCreateCategory={handleCreateCategory}
        />
      )}
    </main>
  )
}

export default App
