# Quickstart: validar la entrada por texto con IA

Guía para comprobar de punta a punta que la feature funciona. Los detalles de los datos están en [data-model.md](./data-model.md) y los de la API en [contracts/openapi.yaml](./contracts/openapi.yaml).

## Prerrequisitos

- .NET 10 SDK y Node 24 LTS (en este entorno WSL todavía no están instalados: `dotnet` y `node` no se encuentran).
- SQL Server accesible; cadena de conexión en la configuración del backend (`ConnectionStrings:Default`).
- Clave de Anthropic en la configuración del backend (`Anthropic:ApiKey`) y modelo Haiku en `Anthropic:Model`. La clave nunca se commitea (usar user-secrets o variables de entorno).
- Docker disponible, para los tests de integración con SQL Server (Testcontainers).
- Un token de desarrollo para el backend: `dotnet user-jwts create --name fede` (y otro con `--name sofi` para probar el aislamiento). El frontend lo toma de la variable `VITE_DEV_TOKEN` o de `localStorage`, hasta que exista la pantalla de login.

## Arranque

```
# Backend
cd backend
dotnet restore
dotnet run

# Frontend (otra terminal)
cd frontend
npm install
npm run dev
```

## Pruebas automáticas

```
dotnet test                                        # unit, contrato, integración (con IA simulada)
npm test                                           # frontend
ANTHROPIC_EVAL_KEY=<clave> dotnet test --filter ExtractionAccuracyEval   # evaluación real de la IA
```

## Escenarios de validación manual

| # | Qué hacer | Resultado esperado | Cubre |
|---|-----------|--------------------|-------|
| 1 | Escribir "Me tomé un café 2500" y enviar | Modal "¿Está todo bien?" con gasto $2.500, Comida, fecha de hoy y sin evento | AC-03, AC-05 |
| 2 | Escribir "El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil" | Evento Dentista, próximo martes 16:00, y gasto $25.000 en Salud con la fecha del evento | AC-02 |
| 3 | Escribir "Mañana tengo reunión con mamá a las 10" | Solo evento, mañana 10:00, sin gasto | AC-04 |
| 4 | Escribir "Cargué la SUBE con 5000" | Gasto $5.000 en Transporte | AC-09 |
| 5 | Escribir "El viernes tengo dentista" | Evento "todo el día"; hora opcional en el modal | FR-002 |
| 6 | Escribir "café 2500 y taxi 4000" | Aviso de que hay más de un gasto; el modal propone solo el primero | FR-005 |
| 7 | Cambiar el monto en el modal y confirmar | Se guarda el monto corregido | FR-006, FR-007 |
| 8 | Cancelar el modal | No se guarda nada (verificar en la base: 0 filas nuevas) | FR-008, Principio I |
| 9 | Escribir "hola, ¿cómo andás?" | Aviso de que no se entendió; no se abre el modal | Edge case |
| 10 | Editar y borrar un gasto guardado | Los cambios se reflejan; el borrado lo quita | FR-007b |
| 11 | Entrar como Sofi y pedir el ítem de Fede por id | 404 | RNF-03, AC-10 |
| 12 | Abrir la pantalla a 360px de ancho | Sin recortes ni scroll horizontal | RNF-02, AC-13 |

## Medición de latencia

Enviar 100 frases seguidas al endpoint `POST /api/proposals` y calcular el percentil 95 de los tiempos; debe ser menor a 3 s (RNF-01, AC-12). El modal debe quedar visible en menos de 5 s desde el envío (RNF-05, AC-15).

## Criterio de listo

- Escenarios 1 a 12 con el resultado esperado.
- `dotnet test` y `npm test` en verde.
- `ExtractionAccuracyEval` con al menos 90 % de acierto en monto y categoría sobre un dataset de al menos 50 frases.
