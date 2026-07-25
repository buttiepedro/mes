# 004 · Servicio `Nexo.Execution` (Capa 3 · ejecución)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-25
> **Implementa:** [03-data-schema.md](../03-data-schema.md) §2.7 y §2.8 · [04-service-contracts.md](../04-service-contracts.md) §2.7
> **Decisiones de producto que materializa:** **PRD-16** (ambos perfiles) · un solo agregado para lote y proyecto

## Qué es la Capa 3

La **Ejecución (Run)**: la instancia viva de una versión de proceso congelada. Un **único agregado**
`Execution` sirve a los dos sabores — **Lote** (objetivo: producto + cantidad) y **Proyecto** (compromiso:
entregable + fecha objetivo + cliente, como **atributos de la ejecución**, no un catálogo de pedidos). El
motor, el DAG y el reloj son idénticos para ambos; lo único que cambia al nacer es el sabor.

## Qué se construyó

| Proyecto | Contenido |
|---|---|
| **Domain** | Agregado `Execution` (los dos sabores), entidades hijas `TaskRun` (unidad de imputación, con precedencias del DAG congeladas), `InputConsumption`, `Evidence`. Materialización de las TaskRun desde un `ProcessSnapshot`, readiness del DAG (FS/SS/FF), invariantes de cierre y de evidencia. 15 eventos de dominio |
| **Application** | 12 comandos (crear, take/start/progress/block/unblock/complete/skip, consumir insumo, adjuntar evidencia, cerrar, cancelar) + 3 queries (snapshot, listar, bandeja de imputación); puerto `IExecutionDbContext` (EF-free); integration events |
| **Infrastructure** | `ExecutionDbContext` (schema `execution`); lecturas con el grafo completo de TaskRun; outbox propio en `execution.outbox_messages` |
| **Api** | Minimal API `/v1/executions` con 14 rutas; scopes `nexo.execution.read \| write` |
| **Tests** | 10 tests de dominio |

**Modelo creado** (`execution`): `executions`, `task_runs`, `task_run_precedences`, `input_consumptions`, `evidence`, `outbox_messages`.

## Nota de proceso: dos agentes, uno caído

Se aplicó desde el inicio la lección de [003](./003-workmodel.md) (partir por capa). El **agente 1**
(Domain + Application) terminó bien y compilaba. El **agente 2** (Infra + Api + Tests) **se colgó** tras
escribir Infrastructure y Api, **antes de los Tests y sin verificar el build**. Se detectó el stall por la
antigüedad del transcript (~34 h sin cambios), se verificó que Infra+Api compilaban (0 errores), y **los
Tests se escribieron a mano** en esta sesión.

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Instanciación desde un `ProcessSnapshot` de entrada**, no leyendo la base de WorkModel | Execution es su propio bounded context y no lee el schema `work`. En integración real, el snapshot de la versión publicada llega por **gRPC** (`ProcessCatalog.GetPublishedVersion`) — esa llamada queda **pendiente** y documentada |
| 2 | **DAG congelado en cada `TaskRun`** (precedencias + copias de obligación/evidencia) | Permite calcular readiness y enforcar reglas de cierre/evidencia sin consultar WorkModel |
| 3 | **Outbox en `execution.outbox_messages`** | Convención "outbox por servicio" ([002](./002-masterdata.md)/[003](./003-workmodel.md)) |
| 4 | **Refs a `master.*`/`work.*`/`config.*` sin FK** | Referencias lógicas; no acoplan migraciones entre contextos |
| 5 | ⭐ **Namespace de tests `ExecutionTests` (top-level, no bajo `Nexo`)** | El tipo `Execution` colisiona con el namespace `Nexo.Execution` (CS0118) si los tests anidan bajo `Nexo`. Un namespace top-level hace que `Execution` resuelva al agregado |
| 6 | **MVP: `Create` funde draft/schedule/release** (nace `Released`); el scheduler de lag por reloj y la bandeja de imputación de hechos huérfanos quedan diferidos | Acota el slice sin cerrar el modelo |
| 7 | **Sin costo** (MOD-17): consumo = cantidad real, sin valorización |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (26 proyectos) | ✅ **0 errores** |
| `dotnet test` (Execution) | ✅ **10/10** |
| Suite completa | ✅ **59/59** (10 Execution + 10 WorkModel + 10 Production + 29 MasterData) |
| `dotnet ef migrations add ExecutionInitial` + update | ✅ Aplicada |
| Tablas creadas | ✅ `execution.{executions, task_runs, task_run_precedences, input_consumptions, evidence, outbox_messages}` |
| Outbox por servicio | ✅ uno en cada schema (`execution`, `master`, `production`, `work`); `platform` vacío |
| API arriba | ✅ `/health/ready` → **200 Healthy** |
| Contrato expuesto | ✅ **14 rutas** `/v1/executions/…` |
| Seguridad | ✅ `GET /v1/executions` sin token → **401** |

### Qué cubren los 10 tests
Materialización de TaskRun + habilitación de solo los nodos iniciales · lote exige producto+cantidad ·
proyecto exige compromiso · proyecto rechaza cantidad objetivo · snapshot vacío rechazado · OEE solo para
lote · **gating FS** (no se inicia una tarea con predecesora sin terminar) · flujo feliz completa y cierra
con progreso 100% · cierre normal rechaza obligatorias abiertas pero el forzado no · **evidencia obligatoria
antes de completar**.

## Pendientes que deja abiertos

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **gRPC WorkModel → Execution** (`GetPublishedVersion`) para obtener el snapshot | Alta | Hoy el snapshot se pasa como input; falta la integración real entre servicios |
| **Relay del outbox → Kafka** | Alta | Sigue pendiente para todos los servicios |
| **Scheduler de lag por reloj + bandeja de imputación de hechos huérfanos** | Media | Diferidos en este slice |
| **Registrar validadores en `Nexo.Production`** | Media | Único servicio que aún no los ejecuta |
