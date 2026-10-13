# Tasks: Entrada por texto con IA y confirmación

**Input**: Design documents from `/specs/001-texto-ia-confirmacion/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: Incluidos. La constitution (Principio IV) exige test-first: cada tarea de pruebas se escribe y debe **fallar** antes de la implementación que la sigue. Cada tarea cita el requisito que cubre (FR/RF/RNF/AC) para la trazabilidad PRD → spec → código, y cada commit debe mencionar esos IDs.

**Organization**: Las tareas se agrupan por historia de usuario. Las fases siguen el orden de prioridad (US1, US2 y US4 son P1; US3 y US5 son P2), no el orden de numeración de la spec.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de la spec a la que pertenece (US1 a US5)
- Todas las rutas son relativas a la raíz del repositorio

## Path Conventions

- Backend: `backend/` · Tests de backend: `backend.Tests/` · Frontend: `frontend/src/` y `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicializar los tres proyectos y la configuración base

- [ ] T001 Crear el proyecto ASP.NET (.NET 10, Minimal APIs) en `backend/SmartAgenda.Api.csproj` con `backend/Program.cs` vacío y las carpetas `Api/`, `Domain/`, `Extraction/`, `Data/`, `Auth/` según plan.md
- [ ] T002 Crear el proyecto de tests xUnit en `backend.Tests/SmartAgenda.Tests.csproj` con referencias a `Microsoft.AspNetCore.Mvc.Testing` y `Testcontainers.MsSql`, y las carpetas `Contract/`, `Integration/`, `Unit/`, `Fakes/`, `Eval/`
- [ ] T003 Crear el proyecto React + Vite + TypeScript (Node 24 LTS) en `frontend/` con Vitest y React Testing Library, y las carpetas `src/components/`, `src/api/`, `tests/`
- [ ] T004 [P] Configurar `backend/appsettings.json` con `ConnectionStrings:Default`, `Anthropic:Model` = `claude-haiku-5-5` y `Anthropic:TimeoutSeconds` = 4; la clave `Anthropic:ApiKey` solo por user-secrets o variable de entorno; agregar `.gitignore` en la raíz (bin, obj, node_modules, `appsettings.Development.json`, `.claude/settings.local.json`, `.claude/worktrees/`)
- [ ] T005 [P] Configurar formato y lint: `backend/.editorconfig` y ESLint + Prettier en `frontend/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura que TODAS las historias necesitan

**⚠️ CRITICAL**: Ninguna historia puede empezar hasta completar esta fase

- [ ] T006 Crear las entidades en `backend/Domain/`: `Category` (`Id`, `UserId` nulo = del sistema, `Name` string (1-40) obligatorio), `Event` (`Id`, `UserId`, `Title` string (1-120) obligatorio, `Date` obligatoria, `Time` nulo = todo el día, `CreatedAt` UTC) y `Expense` (`Id`, `UserId`, `Amount` decimal(18,2) mayor que 0, `CategoryId`, `Date` obligatoria, `CreatedAt` UTC) según data-model.md
- [ ] T007 Crear `backend/Auth/CurrentUser.cs` que lee el `UserId` solo de los claims del token (nunca del cuerpo) y registrar el esquema Bearer en `backend/Program.cs`; solicitudes sin sesión válida o vencida devuelven 401; verificar que el esquema Bearer acepte los tokens de `dotnet user-jwts` (FR-012, RNF-03)
- [ ] T008 Crear `backend/Data/AppDbContext.cs` (EF Core, SQL Server) con filtros globales por `UserId` de `CurrentUser` en `Event`, `Expense` y `Category` propias (las del sistema, `UserId` nulo, visibles para todos), y `UserId` asignado siempre desde `CurrentUser` al guardar (RNF-03, Principio II) — depende de T006 y T007
- [ ] T009 Crear la migración inicial en `backend/Data/Migrations/` con las tablas `Events`, `Expenses`, `Categories`, `Amount` como `decimal(18,2)`, índice único de `Name` por usuario sin distinguir mayúsculas, y la siembra de las categorías del sistema: Comida, Transporte, Salidas, Salud, Otros — depende de T008
- [ ] T010 [P] Crear el manejo de errores en `backend/Api/ErrorHandling.cs` con el formato `{ "code", "message" }` del contrato (`Error`) y respuestas 400, 404, 409, 422 y 504
- [ ] T011 [P] Crear `backend/Extraction/IExtractionService.cs` y los DTOs `Proposal`, `EventInput`, `ExpenseInput` y los códigos de aviso `MULTIPLE_EVENTS`, `MULTIPLE_EXPENSES`, `DEFAULT_CATEGORY`, `AMBIGUOUS_DATE` en `backend/Domain/Proposal.cs` según contracts/openapi.yaml
- [ ] T012 [P] Crear la infraestructura de tests: `backend.Tests/Fakes/TestAuthHandler.cs` (usuarios Fede y Sofi), `backend.Tests/Fakes/FakeExtractionService.cs` y `backend.Tests/Integration/TestHost.cs` (`WebApplicationFactory` con SQL Server en contenedor)
- [ ] T013 [P] Crear `frontend/src/api/client.ts` con `fetch` tipado según contracts/openapi.yaml y mapeo de errores 401, 422 y 504 a mensajes para el usuario

**Checkpoint**: Base lista; las historias pueden empezar

---

## Phase 3: User Story 1 - Registrar un gasto con una frase (Priority: P1) 🎯 MVP

**Goal**: El usuario escribe "Me tomé un café 2500", ve un modal con gasto $2.500 en Comida y, al confirmar, el gasto queda registrado.

**Independent Test**: Enviar una frase solo con gasto, confirmar el modal y verificar que el gasto se guardó con monto y categoría correctos, sin evento (AC-03, AC-09).

### Tests for User Story 1 ⚠️ (escribir primero; deben fallar)

- [ ] T014 [P] [US1] Test de contrato de `POST /api/proposals` (200, 400, 401, 422, 504) en `backend.Tests/Contract/ProposalsContractTests.cs` (FR-001, FR-011)
- [ ] T015 [P] [US1] Test de contrato de `POST /api/entries` con un gasto (201, 400, 401) en `backend.Tests/Contract/EntriesContractTests.cs` (FR-007)
- [ ] T016 [P] [US1] Test de integración: `POST /api/proposals` deja 0 filas nuevas en `Events` y `Expenses` en `backend.Tests/Integration/ProposalsDoNotPersistTests.cs` (FR-007, SC-004, Principio I)
- [ ] T016b [P] [US1] Test de frontend en `frontend/tests/NoSaveWithoutConfirm.test.tsx`: al enviar el texto solo se llama a `/api/proposals`; `/api/entries` se llama únicamente al presionar "Confirmar" y nunca al cancelar o cerrar el modal con Escape (FR-007, SC-004, Principio I)
- [ ] T017 [P] [US1] Test de integración: `POST /api/entries` guarda el gasto con el `UserId` del token e ignora cualquier `userId` del cuerpo; Sofi no ve el gasto de Fede, en `backend.Tests/Integration/EntriesIsolationTests.cs` (FR-010, RNF-03, AC-10)
- [ ] T018 [P] [US1] Tests unitarios de validación del gasto (monto mayor que 0, fecha válida, categoría del sistema o propia del usuario) en `backend.Tests/Unit/ExpenseValidationTests.cs` (FR-009, FR-004b)
- [ ] T019 [P] [US1] Tests unitarios de `ProposalMapper` para el gasto: fecha por defecto = hoy, categoría "Otros" con aviso `DEFAULT_CATEGORY`, primer gasto por orden de aparición con aviso `MULTIPLE_EXPENSES`, en `backend.Tests/Unit/ProposalMappingTests.cs` (FR-003b, FR-005)
- [ ] T020 [P] [US1] Tests de `EntryInput`: texto vacío o solo espacios no se envía, más de 500 caracteres se bloquea con aviso, indicador de carga y bloqueo de reenvío mientras procesa, en `frontend/tests/EntryInput.test.tsx` (FR-001, FR-013)
- [ ] T021 [P] [US1] Test del modal: muestra el gasto propuesto ($2.500, Comida, fecha de hoy), "Confirmar" llama a `/api/entries`, al cambiar el monto se envía el valor corregido y "Cancelar" no hace ninguna llamada, en `frontend/tests/ConfirmModal.expense.test.tsx` (FR-006, FR-008, AC-05)

### Implementation for User Story 1

- [ ] T022 [US1] Crear `backend/Extraction/ExtractionPrompt.cs`: prompt de sistema corto y estable, herramienta `registrar_propuesta` con esquema de `event` y `expense`, fecha `today` y día de la semana, solo las categorías del sistema y las reglas de fechas de Assumptions de spec.md (FR-002, FR-003, FR-004)
- [ ] T023 [US1] Crear `backend/Extraction/AnthropicExtractionService.cs`: llamada a la Messages API con el modelo de `Anthropic:Model`, timeout de 4 s, sin reintentos en el servidor, error 504 si falla (FR-011, RNF-01, RNF-05) — depende de T022
- [ ] T024 [P] [US1] Crear `backend/Extraction/ProposalMapper.cs` (parte de gasto): convierte la salida del modelo en `Proposal`, resuelve categoría por nombre, aplica fecha por defecto y avisos (FR-003b, FR-005) — depende de T011
- [ ] T025 [P] [US1] Crear el dataset `backend.Tests/Eval/phrases.json` con **al menos 50 frases** coloquiales (`text`, `today`, `expectedAmount`, `expectedCategory`, `expectedEvent`): las 6 frases semilla del apéndice de PRD.md más frases solo de evento donde no debe extraerse gasto (RNF-06, AC-16, Principio III)
- [ ] T026 [US1] Crear `backend.Tests/Eval/ExtractionAccuracyEval.cs` con el trait `ExtractionAccuracyEval`, que solo corre si `ANTHROPIC_EVAL_KEY` está definida y exige al menos 90 % de monto y categoría exactos (RNF-06, AC-16) — depende de T023 y T025
- [ ] T027 [US1] Crear `backend/Api/ProposalsEndpoints.cs` con `POST /api/proposals`: valida `text` (1 a 500 caracteres) y `today`, llama a `IExtractionService`, responde 422 si no hay evento ni gasto, y no accede a la base de datos (FR-001, FR-005, FR-011) — depende de T023 y T024
- [ ] T028 [US1] Crear el validador del gasto y `backend/Api/EntriesEndpoints.cs` con `POST /api/entries` (parte de gasto): `UserId` desde `CurrentUser`, validación de monto, fecha y categoría (FR-007, FR-009, FR-010)
- [ ] T029 [P] [US1] Crear `backend/Api/CategoriesEndpoints.cs` con `GET /api/categories` (del sistema más las propias del usuario) (FR-004)
- [ ] T030 [P] [US1] Crear `frontend/src/components/EntryInput.tsx`: campo único de texto, validación de vacío y máximo 500, indicador de carga y botón deshabilitado mientras procesa (FR-001, FR-013)
- [ ] T031 [US1] Crear `frontend/src/components/ConfirmModal.tsx` (modo creación, parte de gasto): título "¿Está todo bien?", monto, categoría y fecha precompletados **y editables**, botones "Confirmar" y "Cancelar" (FR-006, FR-008, AC-05)
- [ ] T032 [US1] Conectar el flujo en `frontend/src/App.tsx`: texto → propuesta → modal → guardado, con mensaje de error que conserva el texto escrito (FR-011) — depende de T030 y T031
- [ ] T032b [US1] En `frontend/src/App.tsx`: ante un 401 conservar el texto escrito, mostrar "Tu sesión venció, iniciá sesión de nuevo" y dejar un punto de redirección al login (la pantalla la aporta la feature de auth) (FR-012)

**Checkpoint**: US1 funcional y probable por sí sola (MVP)

---

## Phase 4: User Story 2 - Registrar un evento y un gasto en una sola frase (Priority: P1)

**Goal**: "El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil" propone Dentista (próximo martes 16:00) y $25.000 en Salud, y ambos se guardan al confirmar.

**Independent Test**: Frase con evento y gasto; el modal muestra título, fecha, hora, monto y categoría; ambos quedan guardados al confirmar (AC-02).

### Tests for User Story 2 ⚠️

- [ ] T033 [P] [US2] Ampliar `backend.Tests/Unit/ProposalMappingTests.cs`: el gasto toma la fecha del evento de la misma frase, evento sin hora = todo el día, primer evento por orden de aparición con aviso `MULTIPLE_EVENTS`, fecha ambigua con `AMBIGUOUS_DATE` (FR-002, FR-003b, FR-005)
- [ ] T034 [P] [US2] Ampliar `backend.Tests/Contract/EntriesContractTests.cs` con evento + gasto: `Title` string (1-120), `Date` válida, `Time` con formato `HH:mm` o nulo (FR-002, FR-009)
- [ ] T035 [P] [US2] Test de integración: confirmar evento y gasto los guarda juntos con el `UserId` de Fede, Sofi no los ve, y si el gasto es inválido no se guarda ninguno, en `backend.Tests/Integration/EntriesEventIsolationTests.cs` (FR-007, FR-010, RNF-03)
- [ ] T036 [P] [US2] Test del modal con evento y gasto: muestra título, fecha y hora opcional ("todo el día" sin hora) y los avisos junto al dato afectado, en `frontend/tests/ConfirmModal.event.test.tsx` (FR-002, FR-014)

### Implementation for User Story 2

- [ ] T037 [US2] Ampliar `backend/Extraction/ProposalMapper.cs` con el evento: título, fecha, hora opcional, fecha del gasto = fecha del evento, avisos de múltiples ítems y fecha ambigua (FR-002, FR-003b, FR-005)
- [ ] T038 [US2] Ampliar `POST /api/entries` en `backend/Api/EntriesEndpoints.cs` para guardar evento y gasto en una sola transacción con su validador de evento (FR-007, FR-009)
- [ ] T039 [US2] Ampliar `frontend/src/components/ConfirmModal.tsx` con la sección de evento y la visualización de avisos (FR-002, FR-014) — depende de T031

**Checkpoint**: US1 y US2 funcionan de forma independiente

---

## Phase 5: User Story 4 - Revisar y corregir antes de guardar (Priority: P1)

**Goal**: El modal valida todos los campos, se maneja por teclado y permite agregar categorías propias.

**Independent Test**: Editar un campo y confirmar guarda el valor corregido; cancelar no guarda nada (AC-05).

### Tests for User Story 4 ⚠️

- [ ] T040 [P] [US4] Tests del modal: el evento es editable (título y fecha); Escape cancela sin llamar a `/api/entries`; monto vacío, negativo o no numérico, título vacío y fecha inválida bloquean "Confirmar", en `frontend/tests/ConfirmModal.edit.test.tsx` (FR-006, FR-008, FR-009)
- [ ] T041 [P] [US4] Test de accesibilidad y diseño: foco atrapado en el modal, uso solo con teclado, sin scroll horizontal a 360px de ancho, en `frontend/tests/ConfirmModal.a11y.test.tsx` (FR-015, RNF-02, AC-13)
- [ ] T042 [P] [US4] Test de contrato e integración de `POST /api/categories`: 201, 400 (`Name` string (1-40)), 409 por nombre repetido sin distinguir mayúsculas; Sofi no ve las categorías propias de Fede; un gasto con categoría de otro usuario devuelve 400, en `backend.Tests/Contract/CategoriesContractTests.cs` (FR-004b, RNF-03)

### Implementation for User Story 4

- [ ] T043 [US4] En `frontend/src/components/ConfirmModal.tsx`: extender la edición al evento, validar todos los campos (monto mayor que 0, fecha válida, título 1-120), cerrar con Escape y atrapar el foco (FR-006, FR-009, FR-015)
- [ ] T044 [US4] Agregar `POST /api/categories` en `backend/Api/CategoriesEndpoints.cs`: `Name` string (1-40), único por usuario sin distinguir mayúsculas, `UserId` desde `CurrentUser` (FR-004b)
- [ ] T045 [US4] Agregar al modal el selector de categoría con alta de una categoría propia (FR-004b) — depende de T043 y T044
- [ ] T046 [P] [US4] Crear `frontend/src/styles.css` mobile-first con ancho mínimo de 360px para el campo de texto y el modal (FR-015, RNF-02)

**Checkpoint**: US1, US2 y US4 completas; el flujo principal cumple el Principio I

---

## Phase 6: User Story 3 - Registrar solo un evento (Priority: P2)

**Goal**: "Mañana tengo reunión con mamá a las 10" propone solo el evento, sin gasto.

**Independent Test**: Frase sin monto; el modal muestra solo el evento y al confirmar no se crea ningún gasto (AC-04).

### Tests for User Story 3 ⚠️

- [ ] T047 [P] [US3] Test de integración: propuesta solo con evento y confirmación sin crear gasto, en `backend.Tests/Integration/EventOnlyTests.cs` (FR-005, AC-04)
- [ ] T048 [P] [US3] Test de integración: un texto sin evento ni gasto ("hola, ¿cómo andás?") devuelve 422 y no abre el modal; `POST /api/entries` con cuerpo sin evento ni gasto devuelve 400, en `backend.Tests/Integration/NothingRecognizedTests.cs` (FR-011)
- [ ] T049 [P] [US3] Test del modal: con propuesta solo de evento no se muestra la sección de gasto, en `frontend/tests/ConfirmModal.eventOnly.test.tsx` (FR-005)

### Implementation for User Story 3

- [ ] T050 [US3] Garantizar en `backend/Api/ProposalsEndpoints.cs` y `backend/Api/EntriesEndpoints.cs` el 422 sin ítems, el 400 en confirmación vacía y `expense` nulo cuando el texto no menciona gasto (FR-005, FR-011)
- [ ] T051 [US3] Ajustar `frontend/src/components/ConfirmModal.tsx` y `frontend/src/App.tsx` para mostrar solo las secciones presentes y un aviso "no entendí" en el caso 422 (FR-005, FR-011)

**Checkpoint**: US3 completa

---

## Phase 7: User Story 5 - Editar o borrar un ítem ya guardado (Priority: P2)

**Goal**: El usuario ve la lista de ítems del mes, edita en el mismo modal y borra con confirmación explícita.

**Independent Test**: Guardar un gasto, editar su monto desde el modal, verificar el cambio; borrarlo con confirmación y verificar que desaparece (SC-006).

### Tests for User Story 5 ⚠️

- [ ] T052 [P] [US5] Tests de contrato de `GET`, `PUT` y `DELETE` en `/api/events` y `/api/expenses` (200, 204, 400, 401, 404), en `backend.Tests/Contract/EventsExpensesContractTests.cs` (FR-007b, FR-007c, FR-007e)
- [ ] T053 [P] [US5] Tests de integración de aislamiento: Fede no puede listar, editar ni borrar ítems de Sofi, y la respuesta es idéntica a la de un ítem inexistente, en `backend.Tests/Integration/SavedItemsIsolationTests.cs` (FR-007d, RNF-03, AC-10)
- [ ] T054 [P] [US5] Tests del frontend: la lista muestra los ítems del mes; editar abre el modal en modo edición con los datos cargados y guarda solo al "Confirmar"; borrar pide confirmación y al rechazarla el ítem sigue guardado, en `frontend/tests/SavedItemsList.test.tsx` (FR-007b, FR-007c, FR-007e)

### Implementation for User Story 5

- [ ] T055 [P] [US5] Crear `backend/Api/EventsEndpoints.cs` con `GET /api/events?from&to`, `PUT /api/events/{id}` y `DELETE /api/events/{id}` (borrado definitivo; 404 si no existe para el usuario) (FR-007b, FR-007c, FR-007d, FR-007e)
- [ ] T056 [P] [US5] Crear `backend/Api/ExpensesEndpoints.cs` con `GET /api/expenses?from&to`, `PUT /api/expenses/{id}` y `DELETE /api/expenses/{id}` con las mismas reglas (FR-007b, FR-007c, FR-007d, FR-007e)
- [ ] T057 [P] [US5] Crear `frontend/src/components/SavedItemsList.tsx`: lista de eventos y gastos del mes con acciones editar y borrar (FR-007e)
- [ ] T058 [US5] Agregar a `frontend/src/components/ConfirmModal.tsx` el modo edición (datos cargados, `PUT` solo desde "Confirmar") y el diálogo de confirmación de borrado (FR-007b, FR-007c) — depende de T043
- [ ] T059 [US5] Integrar la lista y el modo edición en `frontend/src/App.tsx` (FR-007b, FR-007e) — depende de T057 y T058

**Checkpoint**: todas las historias completas

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T060 [P] Test de integración: cada endpoint de contracts/openapi.yaml devuelve 401 sin token o con token vencido y no procesa ni guarda nada, en `backend.Tests/Integration/AuthRequiredTests.cs` (FR-012)
- [ ] T061 [P] Crear `backend.Tests/Eval/LatencyEval.cs` (solo con `ANTHROPIC_EVAL_KEY`): 100 solicitudes consecutivas a `POST /api/proposals` con p95 menor a 3 s (RNF-01, AC-12, SC-002)
- [ ] T062 Revisión de seguridad: confirmar que ningún endpoint lee `userId` del cuerpo, que ninguna consulta evita los filtros por usuario y que la clave de Anthropic no está en el repositorio (RNF-03, Principio II)
- [ ] T063 Ejecutar `dotnet test` y `npm test` y `ANTHROPIC_EVAL_KEY=<clave> dotnet test --filter ExtractionAccuracyEval`; actualizar el apéndice de `PRD.md` con la medición real (la cifra actual de 98,4 % no se pudo verificar en el repositorio) (RNF-06, AC-16)
- [ ] T064 Ejecutar los 12 escenarios de `specs/001-texto-ia-confirmacion/quickstart.md`, incluyendo el modal visible en menos de 5 s (RNF-05, AC-15), y registrar los resultados

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias
- **Foundational (Phase 2)**: depende de Setup; BLOQUEA todas las historias
- **US1 (Phase 3)**: depende de Foundational; es el MVP
- **US2 (Phase 4)**: depende de Foundational; amplía archivos de US1 (`ProposalMapper`, `EntriesEndpoints`, `ConfirmModal`), por lo que conviene hacerla después de US1
- **US4 (Phase 5)**: depende de US1 (el modal ya existe); T043 amplía `ConfirmModal.tsx`
- **US3 (Phase 6)**: depende de US2 (el evento ya se extrae y guarda)
- **US5 (Phase 7)**: depende de US4 (el modal editable) y de las entidades de Foundational
- **Polish (Phase 8)**: depende de las historias deseadas

### Within Each User Story

- Los tests se escriben primero y deben fallar antes de implementar
- Dominio y servicios antes de endpoints; endpoints antes de la integración en el frontend

### Parallel Opportunities

- Setup: T004 y T005 en paralelo
- Foundational: T010, T011, T012 y T013 en paralelo tras T006 a T009
- Dentro de cada historia, todas las tareas de test marcadas [P] corren en paralelo
- US1: T024 y T025 en paralelo con T022; T029 y T030 en paralelo con el resto del backend
- US5: T055, T056 y T057 en paralelo

---

## Parallel Example: User Story 1

```bash
# Tests de US1 en paralelo (archivos distintos):
Task: "Test de contrato de POST /api/proposals en backend.Tests/Contract/ProposalsContractTests.cs"
Task: "Test de integración proposals no persiste en backend.Tests/Integration/ProposalsDoNotPersistTests.cs"
Task: "Tests de EntryInput en frontend/tests/EntryInput.test.tsx"

# Implementación independiente en paralelo:
Task: "ProposalMapper (gasto) en backend/Extraction/ProposalMapper.cs"
Task: "Dataset de evaluación en backend.Tests/Eval/phrases.json"
Task: "EntryInput en frontend/src/components/EntryInput.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 y Phase 2
2. Completar Phase 3 (US1)
3. **DETENERSE y validar**: frase solo con gasto, confirmar y verificar (AC-03, AC-09)
4. Medir la IA con el dataset antes de seguir (Principio III)

### Incremental Delivery

1. Setup + Foundational
2. US1 (MVP) → US2 (evento + gasto) → US4 (revisar y corregir, cierra el Principio I del flujo principal)
3. US3 (solo evento) → US5 (editar y borrar)
4. Polish y medición final

### Notas

- El MVP cumple la constitution: US1 incluye un modal editable con "Confirmar" y "Cancelar", y el endpoint de propuestas no escribe en la base
- El calendario, el presupuesto, el registro/login y los reintentos offline son otras features
- Hacer un commit por tarea o grupo lógico, mencionando los IDs de requisito
