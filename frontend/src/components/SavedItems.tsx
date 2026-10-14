import type { Category, SavedEvent, SavedExpense } from '../api/client'
import { describeItem, type SavedItem } from './itemLabels'

interface SavedItemsProps {
  events: SavedEvent[]
  expenses: SavedExpense[]
  categories: Category[]
  onEdit: (target: SavedItem) => void
  onDelete: (target: SavedItem) => void
}

/** Eventos y gastos guardados del mes, para elegir cuál editar o borrar (FR-007e). */
function SavedItems({ events, expenses, categories, onEdit, onDelete }: SavedItemsProps) {
  const rows: SavedItem[] = [
    ...events.map((item) => ({ kind: 'event' as const, item })),
    ...expenses.map((item) => ({ kind: 'expense' as const, item })),
  ].sort((a, b) => a.item.date.localeCompare(b.item.date))

  return (
    <section aria-label="Ítems del mes">
      <h2>Tus ítems de este mes</h2>
      {rows.length === 0 ? (
        <p>Todavía no hay eventos ni gastos este mes.</p>
      ) : (
        <ul>
          {rows.map((row) => {
            const label = describeItem(row, categories)
            const detail =
              row.kind === 'event'
                ? `${row.item.date} ${row.item.time ?? 'todo el día'}`
                : row.item.date
            return (
              <li key={`${row.kind}-${row.item.id}`}>
                <p>
                  {row.kind === 'event' ? 'Evento: ' : 'Gasto: '}
                  <strong>{row.kind === 'event' ? row.item.title : label.replace('gasto de ', '')}</strong> · {detail}
                </p>
                <div>
                  <button type="button" aria-label={`Editar ${label}`} onClick={() => onEdit(row)}>
                    Editar
                  </button>
                  <button type="button" aria-label={`Borrar ${label}`} onClick={() => onDelete(row)}>
                    Borrar
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

export default SavedItems
