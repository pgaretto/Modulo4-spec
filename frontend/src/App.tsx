import { useCallback, useEffect, useState } from 'react'
import {
  ApiError,
  confirmEntry,
  createCategory,
  createProposal,
  deleteEvent,
  deleteExpense,
  listCategories,
  listEvents,
  listExpenses,
  updateEvent,
  updateExpense,
  userMessage,
  type Category,
  type ConfirmRequest,
  type Proposal,
  type SavedEvent,
  type SavedExpense,
} from './api/client'
import ConfirmModal from './components/ConfirmModal'
import DeleteConfirm from './components/DeleteConfirm'
import EntryInput from './components/EntryInput'
import { describeItem, type SavedItem } from './components/itemLabels'
import SavedItems from './components/SavedItems'

const pad = (n: number) => String(n).padStart(2, '0')
const toDateKey = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`

/** Fecha local del usuario (yyyy-MM-dd): base para "mañana", "el martes que viene", etc. */
function todayLocal(): string {
  return toDateKey(new Date())
}

/** Primer y último día del mes en curso. */
function currentMonth(): { from: string; to: string } {
  const now = new Date()
  return {
    from: toDateKey(new Date(now.getFullYear(), now.getMonth(), 1)),
    to: toDateKey(new Date(now.getFullYear(), now.getMonth() + 1, 0)),
  }
}

/** Eventos y gastos guardados del mes en curso. */
function loadMonth(): Promise<[SavedEvent[], SavedExpense[]]> {
  const { from, to } = currentMonth()
  return Promise.all([listEvents(from, to), listExpenses(from, to)])
}

/** La propuesta con la que se reabre el modal para editar un ítem guardado. */
function proposalFor(target: SavedItem): Proposal {
  return target.kind === 'event'
    ? {
        event: { title: target.item.title, date: target.item.date, time: target.item.time },
        expense: null,
        warnings: [],
      }
    : {
        event: null,
        expense: { amount: target.item.amount, categoryId: target.item.categoryId, date: target.item.date },
        warnings: [],
      }
}

interface AppProps {
  /** Punto de enlace con el login (otra feature): se llama cuando la sesión venció (FR-012). */
  onUnauthorized?: () => void
}

function App({ onUnauthorized }: AppProps) {
  const [proposal, setProposal] = useState<Proposal | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [events, setEvents] = useState<SavedEvent[]>([])
  const [expenses, setExpenses] = useState<SavedExpense[]>([])
  const [error, setError] = useState<string | null>(null)
  const [modalError, setModalError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [editing, setEditing] = useState<SavedItem | null>(null)
  const [deleting, setDeleting] = useState<SavedItem | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  // Cambiar la clave vuelve a montar el campo de texto vacío (solo después de guardar).
  const [entryKey, setEntryKey] = useState(0)

  const handleError = useCallback(
    (err: unknown): string => {
      if (err instanceof ApiError && err.kind === 'unauthorized') onUnauthorized?.()
      return userMessage(err)
    },
    [onUnauthorized],
  )

  // Vuelve a pedir los ítems del mes (después de guardar, editar o borrar).
  const refreshItems = useCallback(async () => {
    try {
      const [savedEvents, savedExpenses] = await loadMonth()
      setEvents(savedEvents)
      setExpenses(savedExpenses)
    } catch (err) {
      handleError(err)
    }
  }, [handleError])

  useEffect(() => {
    // Carga inicial: los ítems del mes y las categorías.
    loadMonth()
      .then(([savedEvents, savedExpenses]) => {
        setEvents(savedEvents)
        setExpenses(savedExpenses)
      })
      .catch(handleError)
    listCategories()
      .then(setCategories)
      .catch(() => undefined) // si falla, se vuelve a intentar al enviar una frase
  }, [handleError])

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
      await refreshItems()
    } catch (err) {
      setModalError(handleError(err))
    } finally {
      setSaving(false)
    }
  }

  // Editar un ítem guardado: mismo modal, y el cambio se guarda solo al confirmar (Principio I).
  const handleEditConfirm = async (entry: ConfirmRequest) => {
    if (!editing) return
    setSaving(true)
    setModalError(null)
    try {
      if (editing.kind === 'event' && entry.event) await updateEvent(editing.item.id, entry.event)
      if (editing.kind === 'expense' && entry.expense) await updateExpense(editing.item.id, entry.expense)
      setEditing(null)
      await refreshItems()
    } catch (err) {
      setModalError(handleError(err))
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleting) return
    setSaving(true)
    setDeleteError(null)
    try {
      if (deleting.kind === 'event') await deleteEvent(deleting.item.id)
      else await deleteExpense(deleting.item.id)
      setDeleting(null)
      await refreshItems()
    } catch (err) {
      setDeleteError(handleError(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <main>
      <h1>Smart-Agenda</h1>
      <EntryInput key={entryKey} onSubmit={handleSubmit} error={error} />
      <SavedItems
        events={events}
        expenses={expenses}
        categories={categories}
        onEdit={(target) => {
          setModalError(null)
          setEditing(target)
        }}
        onDelete={(target) => {
          setDeleteError(null)
          setDeleting(target)
        }}
      />

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
      {editing && (
        <ConfirmModal
          key={`${editing.kind}-${editing.item.id}`}
          proposal={proposalFor(editing)}
          categories={categories}
          saving={saving}
          error={modalError}
          onConfirm={handleEditConfirm}
          onCancel={() => setEditing(null)}
          onCreateCategory={handleCreateCategory}
        />
      )}
      {deleting && (
        <DeleteConfirm
          label={describeItem(deleting, categories)}
          busy={saving}
          error={deleteError}
          onConfirm={handleDeleteConfirm}
          onCancel={() => setDeleting(null)}
        />
      )}
    </main>
  )
}

export default App
