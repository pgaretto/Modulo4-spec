# Feature Specification: Entrada por texto con IA y confirmación

**Feature Branch**: `001-texto-ia-confirmacion`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Entrada por texto + IA + modal: el usuario escribe una frase libre y la app extrae evento y/o gasto, los categoriza y los muestra en un modal de confirmación antes de guardar (RF-04 a RF-08; AC-02 a AC-05, AC-09)."

## Clarifications

### Session 2026-10-13

- Q: Si una frase menciona más de un gasto o más de un evento, ¿qué propone la app? → A: Un solo evento y un solo gasto por frase; si hay más, avisa y propone solo el primero de cada tipo.
- Q: ¿Con qué fecha se registra un gasto cuando la frase no menciona ninguna? → A: La fecha del evento de la frase si hay uno; si no, la de hoy. Siempre editable en el modal.
- Q: ¿Qué hace la app con la hora si el evento tiene fecha pero no hora? → A: El evento queda como "todo el día" y la hora es opcional y editable en el modal.
- Q: ¿La lista de categorías es fija o el usuario puede crear las suyas? → A: Lista inicial fija, pero el usuario puede agregar categorías propias.
- Q: ¿Qué pasa si el usuario quiere borrar o corregir un ítem ya guardado? → A: Esta feature incluye editar y borrar eventos y gastos ya guardados.
- Q: ¿La edición de un ítem guardado pasa por el modal, dado el Principio I? → A: Sí. Editar reabre el mismo modal y guarda solo al confirmar; borrar pide una confirmación explícita. La constitution no cambia.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar un gasto con una frase (Priority: P1)

El usuario escribe en un único campo una frase coloquial con un gasto (ej. "Me tomé un café 2500"). La app interpreta el monto y la categoría, y le muestra un modal con los datos ya completados para que revise y confirme. Al confirmar, el gasto queda registrado.

**Why this priority**: es el valor central del producto: anotar un gasto sin abrir formularios ni llenar campos.

**Independent Test**: se puede probar escribiendo una frase con solo un gasto, confirmando el modal y verificando que el gasto quedó registrado con el monto y la categoría correctos y sin ningún evento creado.

**Acceptance Scenarios**:

1. **Given** el usuario escribe "Me tomé un café 2500", **When** envía el texto, **Then** el modal propone un gasto de $2.500 en la categoría "Comida" y ningún evento.
2. **Given** el usuario escribe "Cargué la SUBE con 5000", **When** envía el texto, **Then** el modal propone un gasto de $5.000 en la categoría "Transporte".
3. **Given** el modal propone un gasto, **When** el usuario presiona "Confirmar", **Then** el gasto se registra y el saldo del mes se actualiza.

---

### User Story 2 - Registrar un evento y un gasto en una sola frase (Priority: P1)

El usuario escribe una frase que combina un evento y un gasto (ej. "El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil"). La app extrae ambos y los muestra juntos en el modal.

**Why this priority**: es el diferencial del producto: unir calendario y plata en una sola entrada.

**Independent Test**: se puede probar con una frase con evento y gasto, verificando en el modal el título, la fecha, la hora, el monto y la categoría, y que ambos quedan guardados al confirmar.

**Acceptance Scenarios**:

1. **Given** el usuario escribe "El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil", **When** envía el texto, **Then** el modal propone el evento "Dentista" para el próximo martes a las 16:00 y un gasto de $25.000 en "Salud".
2. **Given** el usuario escribe "Mañana almuerzo con mamá y gasto 15000", **When** envía el texto, **Then** el modal propone el evento "Almuerzo con mamá" para mañana y un gasto de $15.000 en "Comida".

---

### User Story 3 - Registrar solo un evento (Priority: P2)

El usuario escribe una frase con un evento y sin gasto (ej. "Mañana tengo reunión con mamá a las 10"). La app propone solo el evento.

**Why this priority**: completa la entrada única, aunque aporta menos que el registro de gastos.

**Independent Test**: se puede probar con una frase sin monto, verificando que el modal muestra solo el evento y que al confirmar no se crea ningún gasto.

**Acceptance Scenarios**:

1. **Given** el usuario escribe "Mañana tengo reunión con mamá a las 10", **When** envía el texto, **Then** el modal propone el evento "Reunión con mamá" para mañana a las 10:00 y ningún gasto.

---

### User Story 4 - Revisar y corregir antes de guardar (Priority: P1)

Antes de guardar cualquier cosa, el usuario ve el modal "¿Está todo bien?" con todos los campos precompletados y editables. Puede corregir los datos, confirmar o cancelar. Nada se guarda sin su confirmación.

**Why this priority**: es la garantía de que la IA nunca guarda datos incorrectos por su cuenta.

**Independent Test**: se puede probar editando un campo en el modal, confirmando y verificando que se guarda el valor corregido; y cancelando y verificando que no se guardó nada.

**Acceptance Scenarios**:

1. **Given** el modal está abierto con datos propuestos, **When** el usuario cambia el monto y confirma, **Then** se guarda el monto corregido.
2. **Given** el modal está abierto, **When** el usuario lo cancela, **Then** no se guarda ningún evento ni gasto.
3. **Given** la IA terminó de estructurar la frase, **When** finaliza el procesamiento, **Then** el modal "¿Está todo bien?" se abre automáticamente.

---

### User Story 5 - Editar o borrar un ítem ya guardado (Priority: P2)

El usuario ve la lista de sus eventos y gastos del mes y puede corregir o borrar uno que ya confirmó, por ejemplo si se equivocó de monto o lo cargó por error. Corregir reabre el mismo modal "¿Está todo bien?" con los datos cargados, y el cambio se guarda solo al presionar "Confirmar". Borrar pide una confirmación explícita. El saldo del mes se recalcula con el cambio.

**Why this priority**: sin esto, un error de confirmación queda para siempre y distorsiona el saldo del mes.

**Independent Test**: se puede probar guardando un gasto, editando su monto desde el modal y verificando el nuevo valor; luego borrándolo con confirmación y verificando que desaparece.

**Acceptance Scenarios**:

1. **Given** un gasto guardado de $2.500, **When** el usuario lo abre para editar, cambia el monto a $3.000 y presiona "Confirmar", **Then** el gasto queda en $3.000 y el saldo del mes se recalcula.
2. **Given** el modal de edición abierto, **When** el usuario lo cancela, **Then** el ítem queda sin cambios.
3. **Given** un evento guardado, **When** el usuario lo borra y acepta la confirmación, **Then** deja de aparecer en el calendario y no hay forma de deshacerlo.
4. **Given** un evento guardado, **When** el usuario inicia el borrado y rechaza la confirmación, **Then** el evento sigue guardado.
5. **Given** el usuario "Fede", **When** intenta editar o borrar un ítem de "Sofi", **Then** el sistema responde igual que si el ítem no existiera.

---

### Edge Cases

- ¿Qué pasa si la frase no contiene ni evento ni gasto reconocible? La app avisa que no entendió nada y no abre el modal con datos vacíos como si fueran válidos.
- ¿Qué pasa si el texto tiene un monto pero no se puede deducir la categoría? El modal propone la categoría "Otros", editable, con un aviso.
- ¿Qué pasa si la frase menciona varios gastos o varios eventos (ej. "café 2500 y taxi 4000")? La app avisa y el modal propone solo el primero de cada tipo, según el orden de aparición en el texto; el usuario carga el resto con otra frase.
- ¿Qué pasa si el texto está vacío o solo tiene espacios? No se envía.
- ¿Qué pasa si el texto supera los 500 caracteres? No se envía y se avisa del límite.
- ¿Qué pasa si el servicio de IA no responde dentro de 4 segundos o falla? La app informa el error, conserva el texto escrito y permite reintentar.
- ¿Qué pasa si la fecha es ambigua (ej. "el finde")? El modal propone una fecha concreta según las reglas de Assumptions, editable y con un aviso.
- ¿Qué pasa si la sesión venció o no hay sesión? El sistema rechaza la solicitud sin procesar ni guardar nada y pide al usuario iniciar sesión; el texto escrito se conserva.
- ¿Qué pasa si el usuario edita en el modal un monto no válido (vacío, negativo o no numérico)? El modal no deja confirmar hasta corregirlo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Los usuarios MUST poder ingresar en un único campo de texto libre toda la información de un evento y/o gasto (RF-04).
- **FR-002**: El sistema MUST extraer del texto los datos del evento: título, fecha y hora (RF-05). La hora es opcional: si el texto no la menciona, el evento queda como "todo el día" y el modal permite agregarla.
- **FR-003**: El sistema MUST extraer del texto los datos del gasto: monto y categoría (RF-06).
- **FR-003b**: El gasto MUST tener una fecha: la del evento de la misma frase si hay uno; si no, la fecha de hoy. El modal MUST mostrarla y permitir editarla.
- **FR-004**: El sistema MUST asignar automáticamente cada gasto a una categoría lógica, como Comida, Transporte, Salidas o Salud (RF-08).
- **FR-004b**: El usuario MUST poder agregar categorías propias desde el modal; cada categoría propia es visible únicamente para quien la creó (RNF-03). La asignación automática se evalúa contra la lista inicial (RNF-06); las categorías propias se eligen a mano en el modal. Un gasto MUST rechazarse si referencia una categoría que no es del sistema ni del propio usuario.
- **FR-005**: El sistema MUST crear solo lo que el texto menciona: sin gasto mencionado no propone gasto, y sin evento mencionado no propone evento. Una frase produce como máximo un evento y un gasto; si el texto menciona más de uno del mismo tipo, el sistema MUST avisar al usuario y proponer solo el primero de cada tipo, entendiendo "primero" como el que aparece antes en el texto.
- **FR-006**: El sistema MUST mostrar un modal "¿Está todo bien?" con todos los campos precompletados y editables antes de guardar nada (RF-07).
- **FR-007**: El sistema MUST guardar eventos y gastos únicamente cuando el usuario presiona "Confirmar" en el modal (RF-07).
- **FR-007b**: El usuario MUST poder editar los eventos y gastos que ya guardó. La edición MUST hacerse en el mismo modal "¿Está todo bien?", con los datos cargados, y el cambio MUST guardarse solo al presionar "Confirmar" (Principio I). No pasa de nuevo por la IA y el sistema MUST validar los campos igual que en FR-009.
- **FR-007c**: El usuario MUST poder borrar los eventos y gastos que ya guardó. El borrado MUST pedir una confirmación explícita, es definitivo y no se puede deshacer.
- **FR-007d**: El sistema MUST responder a un intento de leer, editar o borrar un ítem de otro usuario exactamente igual que si el ítem no existiera, sin revelar que existe (RNF-03).
- **FR-007e**: El usuario MUST poder ver la lista de sus eventos y gastos del mes para elegir cuál editar o borrar.
- **FR-008**: El usuario MUST poder cancelar el modal sin que se guarde nada.
- **FR-009**: El sistema MUST validar los campos editados en el modal (monto numérico positivo, fecha válida) antes de permitir confirmar.
- **FR-010**: El sistema MUST asociar los eventos y gastos guardados únicamente al usuario autenticado que los confirmó (RNF-03).
- **FR-011**: El sistema MUST informar al usuario cuando no pueda interpretar el texto o falle el procesamiento, conservando el texto escrito. Se considera falla si la IA no responde dentro de 4 segundos.
- **FR-012**: El sistema MUST rechazar, sin procesar ni guardar nada, toda solicitud sin sesión válida o con sesión vencida, y MUST pedir al usuario iniciar sesión conservando el texto escrito.
- **FR-013**: Mientras la IA procesa el texto, la pantalla MUST mostrar un indicador de carga y MUST impedir un segundo envío del mismo texto.
- **FR-014**: Los avisos de la propuesta (más de un ítem del mismo tipo, categoría "Otros" por no poder deducirla, fecha ambigua) MUST mostrarse dentro del modal, junto al dato afectado, con estos textos exactos: "Había más de un gasto; se propone el primero" (o "Había más de un evento; se propone el primero"), "No pude deducir la categoría; elegí una" y "Fecha estimada; revisala".
- **FR-015**: El modal y la lista de ítems guardados MUST poder usarse solo con teclado (foco dentro del modal, Escape = cancelar) y MUST cumplir el ancho mínimo de 360px sin recortes ni scroll horizontal (RNF-02).

### Key Entities

- **Frase del usuario**: texto libre ingresado en el campo único; origen de la propuesta.
- **Propuesta**: resultado de la interpretación; puede contener un evento, un gasto o ambos, y no está guardada hasta que el usuario confirma.
- **Evento**: título, fecha y hora opcional (sin hora = todo el día); pertenece a un usuario.
- **Gasto**: monto, categoría y fecha; pertenece a un usuario y descuenta del presupuesto del mes.
- **Categoría**: clasificación lógica del gasto. Lista inicial fija (Comida, Transporte, Salidas, Salud, Otros) más las categorías propias que cada usuario agregue, visibles solo para él.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Desde que el usuario envía el texto hasta que ve el modal de confirmación pasan menos de 5 segundos (RNF-05).
- **SC-002**: En un conjunto de 100 solicitudes consecutivas, al menos el 95% de las interpretaciones responde en menos de 3 segundos (RNF-01, AC-12).
- **SC-003**: En un conjunto de al menos 50 frases coloquiales de validación guardado en el repositorio con sus valores esperados, al menos el 90% tiene el monto y la categoría del gasto idénticos a los esperados (RNF-06, AC-16).
- **SC-004**: Ninguna acción de la interfaz crea ni modifica un evento o gasto sin que el usuario presione "Confirmar" en el modal; el texto enviado a la IA nunca se guarda por sí solo.
- **SC-005**: Un usuario puede registrar un gasto simple en un solo campo y una confirmación, sin completar ningún otro campo.
- **SC-006**: Un usuario puede corregir o borrar un ítem guardado en no más de 3 interacciones y ver el saldo del mes actualizado.

## Assumptions

- El usuario ya está registrado y autenticado; el registro y el login son otra feature (RF-11, RF-12). Hasta que exista, esta feature se prueba con usuarios simulados (tests) o con tokens de desarrollo generados con `dotnet user-jwts create`.
- La API acepta datos de su propio usuario autenticado: el modal es el único camino de la interfaz para guardar, pero el servidor no puede distinguir un cliente que llame directo. Esto no expone datos de otros usuarios (RNF-03).
- El presupuesto mensual y el cuadro de saldo se definen en otra feature (RF-03, RF-09, RF-10); aquí el gasto confirmado solo debe quedar disponible para ese cálculo.
- Todo se maneja en una única moneda local; la entrada por voz o audio queda fuera de alcance de la v1.
- Una categoría propia se guarda en el momento de crearla desde el modal, aunque después el usuario cancele el modal; no es un evento ni un gasto, así que no cae bajo la confirmación obligatoria.
- La lista inicial de categorías es Comida, Transporte, Salidas, Salud y Otros (esta última para lo que no encaja en las demás).
- Los reintentos ante falta de conexión (RNF-04) se especifican aparte y, aun así, deben pasar por el modal de confirmación.
- Las fechas relativas se resuelven respecto de la fecha actual del usuario: "mañana" es el día siguiente; "el martes que viene" (o cualquier día de la semana) es la próxima ocurrencia estrictamente posterior a hoy; "el finde" es el próximo sábado; "la semana que viene" es el lunes de la semana siguiente. En todos los casos ambiguos el modal avisa y la fecha es editable.
