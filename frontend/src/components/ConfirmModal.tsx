import { useEffect, useRef, useState, type FormEvent } from 'react'
import type { Category, ConfirmRequest, Proposal } from '../api/client'

interface ConfirmModalProps {
  proposal: Proposal
  categories: Category[]
  saving?: boolean
  error?: string | null
  /** Se llama solo al presionar "Confirmar" (Principio I). */
  onConfirm: (entry: ConfirmRequest) => void
  onCancel: () => void
}

/** Modal "¿Está todo bien?": muestra lo que entendió la IA, editable, antes de guardar nada (RF-07). */
function ConfirmModal({ proposal, categories, saving = false, error, onConfirm, onCancel }: ConfirmModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [validation, setValidation] = useState<string | null>(null)
  const [expense, setExpense] = useState(() =>
    proposal.expense
      ? {
          amount: String(proposal.expense.amount),
          categoryId: proposal.expense.categoryId,
          date: proposal.expense.date,
        }
      : null,
  )

  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    return () => dialog?.close()
  }, [])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const amount = Number(expense?.amount)
    if (expense && (!Number.isFinite(amount) || amount <= 0)) {
      setValidation('Ingresá un monto mayor a 0.')
      return
    }

    setValidation(null)
    onConfirm({
      event: null,
      expense: expense && { amount, categoryId: expense.categoryId, date: expense.date },
    })
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
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Fecha
              <input
                type="date"
                value={expense.date}
                onChange={(e) => setExpense({ ...expense, date: e.target.value })}
              />
            </label>
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
