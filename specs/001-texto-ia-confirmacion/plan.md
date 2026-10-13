# Implementation Plan: Entrada por texto con IA y confirmación

**Branch**: `001-texto-ia-confirmacion` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-texto-ia-confirmacion/spec.md`

## Summary

El usuario escribe una frase libre; el backend la envía a Claude Haiku, que devuelve un evento y/o un gasto estructurados (una **propuesta** que no se guarda). El frontend muestra la propuesta en el modal "¿Está todo bien?", editable. Solo al presionar "Confirmar" el frontend llama al endpoint de confirmación, que valida y persiste con el `UserId` del token. La misma feature incluye listar, editar y borrar ítems ya guardados y agregar categorías propias.

Enfoque técnico: ASP.NET (.NET 10) con Minimal APIs y EF Core sobre SQL Server; la extracción de IA queda detrás de una interfaz (`IExtractionService`) para poder usar un doble en los tests y una implementación real en la evaluación; frontend React + Vite con TypeScript. El aislamiento entre usuarios se aplica en el backend con filtros globales de EF Core más `UserId` explícito en cada escritura.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 sobre Node 24 LTS (frontend)

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core (proveedor SQL Server), SDK/cliente HTTP de la Messages API de Anthropic (validación manual, sin librería extra); React 19, Vite

**Storage**: SQL Server (tablas `Events`, `Expenses`, `Categories`); montos en `decimal(18,2)`

**Testing**: xUnit + `WebApplicationFactory` + Testcontainers para SQL Server (backend); Vitest + React Testing Library (frontend). La evaluación de la IA es una prueba aparte etiquetada `ExtractionAccuracyEval`

**Target Platform**: web responsive, mínimo 360px de ancho (RNF-02); servidor Linux/Windows con .NET 10

**Project Type**: web application (frontend + backend)

**Performance Goals**: extracción de IA p95 < 3 s (RNF-01); modal visible < 5 s desde el envío (RNF-05)

**Constraints**: una sola moneda; sin voz/audio; sin integraciones externas salvo Anthropic; nada se persiste sin confirmación (Principio I); 0 registros cruzados entre usuarios (Principio II)

**Scale/Scope**: v1 con pocos usuarios; un campo de texto, un modal, una lista mínima de ítems del mes para editar/borrar

**Unknowns resueltos** en [research.md](./research.md): ver decisiones D1 a D9.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cumplimiento en el plan | Estado |
|-----------|-------------------------|--------|
| I. Confirmación obligatoria | `POST /api/proposals` nunca escribe en la base. La única ruta que crea eventos y gastos es `POST /api/entries`, invocada solo desde el botón "Confirmar" del modal. La edición de ítems guardados reutiliza el mismo modal (`PUT` se llama solo desde su "Confirmar"); el borrado pide confirmación explícita. Hay un test que verifica que tras `proposals` no hay filas nuevas. | Pasa |
| II. Aislamiento entre usuarios | `UserId` se toma siempre del token, nunca del cuerpo. Filtro global de EF Core por `UserId`; `Categories` propias filtradas igual. Tests Fede/Sofi para cada endpoint (lectura, edición, borrado). | Pasa |
| III. Calidad de IA medida | Se crea `backend.Tests/Eval/phrases.json` (≥ 50 frases) y la prueba `ExtractionAccuracyEval`; timeout de IA y mediciones de latencia en el plan de pruebas. | Pasa (con tarea de crear el dataset, ver nota) |
| IV. Test-first trazable | Cada tarea referencia FR/RF/RNF/AC; los tests de contrato salen de `contracts/openapi.yaml`. | Pasa |
| V. Alcance v1 | Un campo, una moneda, sin voz, sin integraciones; Minimal APIs, sin capas extra; modelo Haiku. | Pasa |

**Nota sobre el dataset**: el PRD indica que `backend.Tests/Eval/phrases.json` ya existe con 63 frases y una medición de 98,4 %, pero el repositorio no tiene código ni ese archivo. Este plan lo trata como un entregable a crear (las 6 frases semilla del PRD más las frases restantes), y no como algo ya medido.

**Re-evaluación post-diseño**: los contratos y el modelo de datos mantienen todos los gates en "Pasa". No hay violaciones que justificar.

## Project Structure

### Documentation (this feature)

```text
specs/001-texto-ia-confirmacion/
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/
│   └── openapi.yaml     # Fase 1
├── checklists/
│   └── requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks, no lo crea /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Program.cs                      # composición, auth, endpoints
├── Api/
│   ├── ProposalsEndpoints.cs       # POST /api/proposals
│   ├── EntriesEndpoints.cs         # POST /api/entries (confirmación)
│   ├── EventsEndpoints.cs          # GET/PUT/DELETE
│   ├── ExpensesEndpoints.cs        # GET/PUT/DELETE
│   └── CategoriesEndpoints.cs      # GET/POST
├── Domain/                         # Event, Expense, Category, Proposal
├── Extraction/
│   ├── IExtractionService.cs
│   ├── AnthropicExtractionService.cs
│   └── ExtractionPrompt.cs
├── Data/
│   ├── AppDbContext.cs             # filtros globales por UserId
│   └── Migrations/
└── Auth/
    └── CurrentUser.cs              # lee el UserId de los claims

backend.Tests/
├── Contract/                       # contra openapi.yaml
├── Integration/                    # aislamiento Fede/Sofi, "proposals no persiste"
├── Unit/                           # validación, reglas de fecha y categoría
├── Fakes/FakeExtractionService.cs
└── Eval/
    ├── phrases.json                # dataset (≥ 50 frases)
    └── ExtractionAccuracyEval.cs   # requiere ANTHROPIC_EVAL_KEY

frontend/
├── src/
│   ├── components/
│   │   ├── EntryInput.tsx          # campo único de texto
│   │   ├── ConfirmModal.tsx        # "¿Está todo bien?" (modo creación y modo edición)
│   │   └── SavedItemsList.tsx      # lista mínima para editar/borrar
│   ├── api/client.ts               # fetch tipado según el contrato
│   └── App.tsx
└── tests/                          # Vitest + Testing Library
```

**Structure Decision**: web application con `backend/`, `backend.Tests/` y `frontend/` en la raíz, alineado con los comandos de `AGENTS.md` y con la ruta del dataset del PRD. El calendario y el cuadro de presupuesto son otra feature y no se crean aquí.

## Complexity Tracking

Sin violaciones de la constitution; no hay nada que justificar.
