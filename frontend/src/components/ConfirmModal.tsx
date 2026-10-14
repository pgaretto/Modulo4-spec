import { useEffect, useRef, useState, type FormEvent } from 'react'
import type { Category, ConfirmRequest, Proposal, ProposalWarning } from '../api/client'

interface ConfirmModalProps {
  proposal: Proposal
  categories: Category[]
  saving?: boolean
  error?: string | null
  /** Se llama solo al presionar "Confirmar" (Principio I). */
  onConfirm: (entry: ConfirmRequest) => void
  onCancel: () => void
  /** Alta de una categoría propia (FR-004b); si falta, el modal no ofrece agregar. */
  onCreateCategory?: (name: string) => Promise<Category>
}

/** Textos exactos de los avisos (FR-014). */
const WARNING_TEXT: Record<ProposalWarning, string> = {
  MULTIPLE_EVENTS: 'Había más de un evento; se propone el primero',
  MULTIPLE_EXPENSES: 'Había más de un gasto; se propone el primero',
  DEFAULT_CATEGORY: 'No pude deducir la categoría; elegí una',
  AMBIGUOUS_DATE: 'Fecha estimada; revisala',
}

function Warnings({ codes }: { codes: ProposalWarning[] }) {
  return codes.map((code) => (
    <p key={code} role="note">
      {WARNING_TEXT[code]}
    </p>
  ))
}

const isValidDate = (value: string) => value !== '' && !Number.isNaN(Date.parse(value))

/** Modal "¿Está todo bien?": muestra lo que entendió la IA, editable, antes de guardar nada (RF-07). */
function ConfirmModal({
  proposal,
  categories,
  saving = false,
  error,
  onConfirm,
  onCancel,
  onCreateCategory,
}: ConfirmModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [validation, setValidation] = useState<string | null>(null)
  const [event, setEvent] = useState(() =>
    proposal.event
      ? { title: proposal.event.title, date: proposal.event.date, time: proposal.event.time ?? '' }
      : null,
  )
  const [expense, setExpense] = useState(() =>
    proposal.expense
      ? {
          amount: String(proposal.expense.amount),
          categoryId: proposal.expense.categoryId,
          date: proposal.expense.date,
        }
      : null,
  )
  const [created, setCreated] = useState<Category[]>([])
  const [addingCategory, setAddingCategory] = useState(false)
  const [newCategory, setNewCategory] = useState('')

  const options = [...categories, ...created.filter((c) => !categories.some((k) => k.id === c.id))]

  // Los avisos van junto al dato afectado; los de fecha, junto al evento (o al gasto si no hay evento).
  const eventWarnings: ProposalWarning[] = proposal.warnings.filter(
    (w) => w === 'MULTIPLE_EVENTS' || (w === 'AMBIGUOUS_DATE' && proposal.event),
  )
  const expenseWarnings: ProposalWarning[] = proposal.warnings.filter(
    (w) => w === 'MULTIPLE_EXPENSES' || w === 'DEFAULT_CATEGORY' || (w === 'AMBIGUOUS_DATE' && !proposal.event),
  )

  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    return () => dialog?.close()
  }, [])

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()

    if (event && !event.title.trim()) return setValidation('Ingresá un título.')
    if ((event && !isValidDate(event.date)) || (expense && !isValidDate(expense.date))) {
      return setValidation('Ingresá una fecha válida.')
    }
    const amount = Number(expense?.amount)
    if (expense && (expense.amount.trim() === '' || !Number.isFinite(amount) || amount <= 0)) {
      return setValidation('Ingresá un monto mayor a 0.')
    }

    setValidation(null)
    onConfirm({
      event: event && { title: event.title.trim(), date: event.date, time: event.time || null },
      expense: expense && { amount, categoryId: expense.categoryId, date: expense.date },
    })
  }

  const handleCreateCategory = async () => {
    const name = newCategory.trim()
    if (!name || !onCreateCategory || !expense) return

    try {
      const category = await onCreateCategory(name)
      setCreated((list) => [...list, category])
      setExpense({ ...expense, categoryId: category.id })
      setNewCategory('')
      setAddingCategory(false)
      setValidation(null)
    } catch {
      setValidation('No se pudo crear la categoría. Revisá que el nombre no exista.')
    }
  }

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="confirm-title"
      onCancel={(e) => {
        // Escape cancela sin guardar.
        e.preventDefault()
        onCancel()
      }}
    >
      <form onSubmit={handleSubmit} noValidate>
        <h2 id="confirm-title">¿Está todo bien?</h2>

        {event && (
          <fieldset>
            <legend>Evento</legend>
            <label>
              Título
              <input
                type="text"
                value={event.title}
                maxLength={120}
                onChange={(e) => setEvent({ ...event, title: e.target.value })}
              />
            </label>
            <label>
              Fecha
              <input type="date" value={event.date} onChange={(e) => setEvent({ ...event, date: e.target.value })} />
            </label>
            <label>
              Hora (opcional)
              <input type="time" value={event.time} onChange={(e) => setEvent({ ...event, time: e.target.value })} />
            </label>
            {!event.time && <p>Sin hora: todo el día</p>}
            <Warnings codes={eventWarnings} />
          </fieldset>
        )}

        {expense && (
          <fieldset>
            <legend>Gasto</legend>
            <label>
              Monto
              <input
                type="number"
                inputMode="decimal"
                step="any"
                value={expense.amount}
                onChange={(e) => setExpense({ ...expense, amount: e.target.value })}
              />
            </label>
            <label>
              Categoría
              <select
                value={expense.categoryId}
                onChange={(e) => setExpense({ ...expense, categoryId: e.target.value })}
              >
                {options.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            {onCreateCategory &&
              (addingCategory ? (
                <div>
                  <label>
                    Nueva categoría
                    <input
                      type="text"
                      value={newCategory}
                      maxLength={40}
                      onChange={(e) => setNewCategory(e.target.value)}
                      onKeyDown={(e) => {
                        // Enter guarda la categoría, no confirma el modal.
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          void handleCreateCategory()
                        }
                      }}
                    />
                  </label>
                  <button type="button" onClick={() => void handleCreateCategory()}>
                    Guardar categoría
                  </button>
                </div>
              ) : (
                <button type="button" onClick={() => setAddingCategory(true)}>
                  Agregar categoría
                </button>
              ))}
            <label>
              Fecha
              <input
                type="date"
                value={expense.date}
                onChange={(e) => setExpense({ ...expense, date: e.target.value })}
              />
            </label>
            <Warnings codes={expenseWarnings} />
          </fieldset>
        )}

        {(validation ?? error) && <p role="alert">{validation ?? error}</p>}

        <div>
          <button type="submit" disabled={saving}>
            Confirmar
          </button>
          <button type="button" onClick={onCancel} disabled={saving}>
            Cancelar
          </button>
        </div>
      </form>
    </dialog>
  )
}

export default ConfirmModal
