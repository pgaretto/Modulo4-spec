# Research: Entrada por texto con IA y confirmación

Todas las decisiones técnicas abiertas del plan. No quedan `NEEDS CLARIFICATION`.

## D1 - Estilo de API: Minimal APIs

- **Decision**: ASP.NET Core Minimal APIs agrupadas por recurso.
- **Rationale**: son pocos endpoints y sin lógica de filtros MVC; menos código (Principio V).
- **Alternatives**: controladores MVC (más ceremonia sin beneficio en v1).

## D2 - Propuesta sin estado, confirmación como única vía de escritura

- **Decision**: `POST /api/proposals` devuelve la propuesta y no toca la base; `POST /api/entries` recibe los datos finales editados por el usuario, los valida y los guarda.
- **Rationale**: hace que "nada se guarda sin confirmar" (Principio I) sea verificable con un test simple: contar filas antes y después de `proposals`. No hace falta guardar propuestas ni expirarlas.
- **Alternatives**: persistir propuestas con un `proposalId` y exigirlo en la confirmación (más fuerte contra clientes maliciosos, pero agrega tabla, expiración y limpieza; un usuario solo puede dañar sus propios datos, así que no justifica la complejidad en v1).

## D3 - Extracción con Claude Haiku mediante salida estructurada

- **Decision**: llamar a la Messages API con el modelo Haiku, forzando una herramienta `registrar_propuesta` cuyo esquema JSON define `event` (título, fecha, hora opcional) y `expense` (monto, categoría). El modelo se lee de configuración (`Anthropic:Model`), por defecto `claude-haiku-5-5`.
- **Rationale**: el esquema obliga a una salida parseable y reduce respuestas inventadas; el modelo chico cumple la restricción de costo y latencia (RNF-01).
- **Alternatives**: pedir JSON libre en el texto (frágil); un modelo mayor (más caro y lento sin necesidad medida).

## D4 - Presupuesto de latencia

- **Decision**: prompt de sistema corto y estable (candidato a prompt caching), `max_tokens` bajo, timeout de la llamada a la IA de 4 s y sin reintentos automáticos en el servidor. Si vence, se devuelve error y el frontend conserva el texto y ofrece reintentar.
- **Rationale**: RNF-01 pide p95 < 3 s en la IA y RNF-05 < 5 s hasta el modal; el timeout de 4 s deja margen de ~1 s para red y render. Un reintento del servidor rompería el límite de 5 s.
- **Alternatives**: reintentar en el servidor (mejora la tasa de éxito pero excede RNF-05).

## D5 - Fechas relativas

- **Decision**: el cliente envía `today` (fecha local `yyyy-MM-dd`) en cada solicitud; el prompt incluye esa fecha y el día de la semana. El modelo devuelve fecha absoluta `yyyy-MM-dd` y hora `HH:mm` opcional.
- **Rationale**: "mañana" o "el martes que viene" dependen de la fecha del usuario, no del servidor (supuesto de la spec). Evita manejar zonas horarias en el servidor.
- **Alternatives**: enviar zona horaria IANA y calcular en el servidor (más piezas sin ganancia).

## D6 - Aislamiento de datos

- **Decision**: el `UserId` sale únicamente de los claims del token (`CurrentUser`); las entidades llevan `UserId`; `AppDbContext` aplica filtros globales por usuario; escrituras y borrados buscan por `Id` + `UserId` y devuelven 404 si no existe para ese usuario.
- **Rationale**: defensa en dos capas, siempre en el backend (Principio II). Devolver 404 en vez de 403 no revela que el recurso existe.
- **Alternatives**: filtrar a mano en cada consulta (se olvida fácil); Row-Level Security de SQL Server (válido, pero más infraestructura; se puede sumar luego).

## D7 - Autenticación fuera de esta feature

- **Decision**: esta feature consume un usuario autenticado vía esquema Bearer y expone `CurrentUser`; el registro y el login (RF-11, RF-12) son otra feature. En los tests se usa un handler de autenticación de prueba con usuarios Fede y Sofi.
- **Rationale**: respeta la frontera de la spec y permite probar el aislamiento sin esperar al login.
- **Alternatives**: construir login mínimo aquí (amplía el alcance).

## D8 - Categorías

- **Decision**: tabla `Categories` con `UserId` nulo para las del sistema (Comida, Transporte, Salidas, Salud, Otros, sembradas por migración) y `UserId` del dueño para las propias. Nombre único por usuario sin distinguir mayúsculas. La IA solo recibe y elige entre las del sistema; las propias se eligen a mano en el modal.
- **Rationale**: mantiene medible el 90 % (RNF-06) y cumple FR-004b y el aislamiento.
- **Alternatives**: pasar las categorías propias a la IA (cambiaría el prompt por usuario y complicaría la evaluación).

## D9 - Dataset y prueba de evaluación

- **Decision**: crear `backend.Tests/Eval/phrases.json` con al menos 50 frases (las 6 semilla del PRD más el resto, incluyendo frases solo de evento donde no debe extraerse gasto) y la prueba `ExtractionAccuracyEval`, que solo corre con `ANTHROPIC_EVAL_KEY` definida y se excluye del `dotnet test` normal. Criterio: monto y categoría exactos en ≥ 90 % de las frases.
- **Rationale**: el PRD cita ese archivo y una medición (63 frases, 98,4 %), pero no existe en el repositorio; hay que crearlo y volver a medir.
- **Alternatives**: confiar en la medición del PRD (no verificable).

## D10 - Frontend

- **Decision**: React + Vite con TypeScript; `fetch` con un cliente tipado según el contrato; estado local con hooks, sin librería de estado; modal accesible (foco atrapado, cierre con Escape = cancelar). Diseño mobile-first con un mínimo de 360px.
- **Rationale**: tamaño de la feature y Principio V; RNF-02.
- **Alternatives**: Redux/React Query (innecesarios para un flujo de dos pantallas).
