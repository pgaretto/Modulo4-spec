# Smart-Agenda Constitution

## Core Principles

### I. Confirmación obligatoria antes de persistir (NON-NEGOTIABLE)

Ningún evento ni gasto MUST guardarse sin pasar antes por el modal de confirmación "¿Está todo bien?"
(RF-07). La IA solo propone; el usuario decide. Ningún flujo, incluidos los reintentos offline
(RNF-04), MUST saltear el modal ni persistir datos no confirmados.

**Rationale**: es la mitigación central del riesgo de que la IA interprete mal o invente datos.

### II. Aislamiento de datos entre usuarios (NON-NEGOTIABLE)

Toda consulta y escritura MUST filtrarse por el usuario autenticado, y ese filtro MUST aplicarse en
el backend; nunca alcanza con aplicarlo solo en el frontend (RNF-03). Cada funcionalidad que lea o
escriba eventos, gastos o presupuestos MUST incluir tests que demuestren que un usuario (Fede) no
puede leer ni modificar datos de otro (Sofi). El objetivo es 0 registros cruzados.

**Rationale**: los datos financieros y de agenda son privados; una sola fuga invalida el producto.

### III. Calidad de la IA medida, no supuesta

Todo cambio de prompt, modelo o lógica de extracción (RF-05, RF-06, RF-08) MUST validarse contra el
dataset de evaluación `backend.Tests/Eval/phrases.json` (RNF-06): al menos 50 frases y al menos 90%
de acierto exacto en monto y categoría. La latencia MUST cumplir RNF-01 (p95 < 3 s en la IA) y
RNF-05 (< 5 s hasta que aparece el modal). Una regresión en el dataset o en la latencia MUST
bloquear el cambio.

**Rationale**: la calidad percibida depende de que la IA entienda el lenguaje coloquial, y eso solo
se sostiene con una medición objetiva y repetible.

### IV. Test-first con criterios de aceptación trazables

Cada requisito (RF/RNF) MUST implementarse con tests escritos antes del código y derivados de sus
criterios de aceptación AC-xx del PRD (`npm test` en frontend, `dotnet test` en backend). Cada tarea
y cada commit MUST referenciar el ID del requisito que cubre, para mantener la trazabilidad
PRD → spec → código.

**Rationale**: la trazabilidad permite verificar que lo construido es lo que se pidió.

### V. Alcance v1 acotado y simplicidad

La v1 MUST limitarse a: un único campo de texto libre, una sola moneda local, sin entrada por
voz/audio y sin integraciones externas (Google/Apple Calendar, bancos o billeteras). Se MUST preferir
la solución más simple y el modelo más chico y económico (Claude Haiku). Cualquier ampliación de
alcance MUST registrarse primero en el PRD.

**Rationale**: evita complejidad, costos y latencias que comprometen los objetivos de la v1.

## Restricciones Adicionales

- Stack: frontend React + Vite (Node 24 LTS); backend ASP.NET (.NET 10); base de datos SQL Server;
  IA con Anthropic, modelo Claude Haiku.
- Diseño responsive: la pantalla MUST funcionar desde 360px de ancho sin recortes ni scroll
  horizontal (RNF-02).
- Resiliencia: los envíos fallidos por falta de conexión MUST guardarse localmente y reintentarse
  hasta 5 veces; si fallan, MUST quedar pendientes de reintento manual con aviso al usuario
  (RNF-04), siempre respetando el Principio I.
- Las credenciales y claves (por ejemplo `ANTHROPIC_EVAL_KEY`) MUST mantenerse fuera del repositorio.

## Flujo de Desarrollo y Quality Gates

- Todo cambio MUST pasar `npm test` y `dotnet test` antes de integrarse.
- Los cambios que toquen prompts, modelo o extracción MUST correr la evaluación
  (`dotnet test --filter ExtractionAccuracyEval`) y cumplir el Principio III.
- Los cambios que toquen acceso a datos MUST incluir tests de aislamiento entre usuarios
  (Principio II).
- Los commits MUST seguir Conventional Commits y mencionar el ID de requisito cubierto.
- Toda revisión MUST verificar el cumplimiento de los cinco principios.

## Governance

Esta constitution prevalece sobre cualquier otra práctica del proyecto. Las enmiendas MUST
documentarse, justificarse y reflejarse en esta versión, con un plan de migración si afectan código
existente. El versionado sigue SemVer: MAJOR por eliminar o redefinir principios de forma
incompatible, MINOR por agregar principios o ampliar guía de forma sustancial, PATCH por
aclaraciones y correcciones de redacción. Cada PR y revisión MUST verificar el cumplimiento de los
principios, y toda complejidad adicional MUST justificarse. La guía operativa para agentes está en
`AGENTS.md`.

**Version**: 1.0.0 | **Ratified**: 2026-10-13 | **Last Amended**: 2026-10-13
