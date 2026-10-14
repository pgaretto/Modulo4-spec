import { useEffect, useRef } from 'react'

interface DeleteConfirmProps {
  /** Qué se va a borrar, por ejemplo "Dentista" o "gasto de $25.000 (Salud)". */
  label: string
  busy?: boolean
  error?: string | null
  onConfirm: () => void
  onCancel: () => void
}

/** Confirmación explícita del borrado, que es definitivo (FR-007c). */
function DeleteConfirm({ label, busy = false, error, onConfirm, onCancel }: DeleteConfirmProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    return () => dialog?.close()
  }, [])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="delete-title"
      onCancel={(e) => {
        e.preventDefault()
        onCancel()
      }}
    >
      <h2 id="delete-title">¿Borrar {label}?</h2>
      <p>No se puede deshacer.</p>
      {error && <p role="alert">{error}</p>}
      <div>
        <button type="button" onClick={onConfirm} disabled={busy}>
          Sí, borrar
        </button>
        <button type="button" onClick={onCancel} disabled={busy}>
          Cancelar
        </button>
      </div>
    </dialog>
  )
}

export default DeleteConfirm
