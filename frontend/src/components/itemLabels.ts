import type { Category, SavedEvent, SavedExpense } from '../api/client'

export type SavedItem =
  | { kind: 'event'; item: SavedEvent }
  | { kind: 'expense'; item: SavedExpense }

export const money = (amount: number) => `$${amount.toLocaleString('es-AR')}`

/** Descripción corta de un ítem, usada en la lista y en los nombres accesibles de sus botones. */
export function describeItem(target: SavedItem, categories: Category[]): string {
  if (target.kind === 'event') return target.item.title
  const category = categories.find((c) => c.id === target.item.categoryId)?.name
  return `gasto de ${money(target.item.amount)}${category ? ` (${category})` : ''}`
}
