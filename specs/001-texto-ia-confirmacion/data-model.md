# Data Model: Entrada por texto con IA y confirmación

Nota de implementación: `UserId` es un `string` (el claim `sub` del token) y no un `guid`, porque la identidad la define la feature de login y los tokens de `dotnet user-jwts` usan el nombre como `sub`.

Todas las entidades persistidas llevan `UserId` y se filtran por el usuario autenticado (RNF-03). La **Propuesta** y la **Frase** no se persisten.

## Category

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | guid | PK |
| `UserId` | guid, nulo | nulo = categoría del sistema; con valor = categoría propia de ese usuario |
| `Name` | string (1-40) | obligatorio; único por usuario sin distinguir mayúsculas; las del sistema son únicas entre sí |

Sembradas: Comida, Transporte, Salidas, Salud, Otros. Las categorías del sistema no se editan ni se borran. Un usuario ve las del sistema más las suyas.

## Event

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | guid | PK |
| `UserId` | guid | obligatorio, tomado del token |
| `Title` | string (1-120) | obligatorio |
| `Date` | date | obligatoria, válida |
| `Time` | time, nulo | nulo = todo el día |
| `CreatedAt` | datetime UTC | automático |

## Expense

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | guid | PK |
| `UserId` | guid | obligatorio, tomado del token |
| `Amount` | decimal(18,2) | obligatorio, mayor que 0 |
| `CategoryId` | guid | FK a `Category`; debe ser del sistema o del mismo usuario |
| `Date` | date | obligatoria; por defecto la del evento de la frase o la de hoy |
| `CreatedAt` | datetime UTC | automático |

Moneda única, sin campo de moneda.

## Propuesta (no persistida)

Respuesta de `POST /api/proposals`:

| Campo | Tipo | Reglas |
|-------|------|--------|
| `event` | objeto, nulo | `title`, `date`, `time` (opcional); nulo si el texto no menciona evento |
| `expense` | objeto, nulo | `amount`, `categoryId`, `date`; nulo si el texto no menciona gasto |
| `warnings` | lista de códigos | por ejemplo `MULTIPLE_EVENTS`, `MULTIPLE_EXPENSES`, `DEFAULT_CATEGORY`, `AMBIGUOUS_DATE` |

Máximo un evento y un gasto. Si ambos son nulos, el endpoint responde 422 y no se abre el modal.

## Relaciones y reglas transversales

- `Expense.CategoryId` → `Category.Id`. Un gasto no puede referenciar una categoría propia de otro usuario.
- Un gasto creado desde una frase con evento toma por defecto la fecha del evento; el usuario puede cambiarla en el modal.
- El saldo del mes (otra feature) se calcula sumando `Expense.Amount` por mes de `Date`; borrar o editar un gasto cambia ese cálculo.

## Transiciones de estado

Frase → (IA) → Propuesta → (el usuario confirma, con o sin ediciones) → Evento y/o Gasto guardados. Cancelar descarta la propuesta sin escribir nada. Los ítems guardados se pueden editar y borrar (borrado definitivo).
