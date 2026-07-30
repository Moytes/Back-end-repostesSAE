# Back-end-RepostesSAE — Microservicio de Reportes y Evaluaciones Clínicas

Microservicio backend (.NET / ASP.NET Core) para canalizaciones, evaluaciones psicopedagógicas, sesiones psicológicas, tamizajes **TEA** y reportes agregados dentro del ecosistema SAE de USEBEQ.

> Este servicio absorbió lo que antes era un microservicio Rust/Axum independiente (`back-end-clinical`). Se consolidó aquí para mantener un solo stack (.NET) en todo el backend.

## Stack

- ASP.NET Core (.NET 10)
- Dapper + Npgsql sobre PostgreSQL (Neon) — acceso a datos por SQL directo, sin ORM de por medio
- Autenticación JWT Bearer (mismo secreto/issuer/audience que `backend-core`)

## Endpoints

| Controlador | Ruta base | Qué hace |
|---|---|---|
| `CanalizacionesController` | `/api/canalizaciones` | Registro y seguimiento de canalizaciones a especialistas |
| `EvaluacionesController` | `/api/clinical/evaluaciones-psicopedagogicas` | Evaluaciones psicopedagógicas completas |
| `SesionesController` | `/api/clinical/alumnos/{id}/sesiones`, `/api/clinical/sesiones/{id}` | Sesiones psicológicas por alumno |
| `TeaController` | `/api/clinical/health`, `/api/clinical/evaluate/tea`, `/api/clinical/history/tea` | Tamizaje TEA (cálculo de puntaje + historial) |
| `ReportesController` | `/api/clinical/reportes` | Reportes agregados (alertas TEA, resumen CIE) para dashboards |

Todos (excepto `health`) requieren JWT válido; varios además exigen rol `ESPECIALISTA_PSI` y validan que el alumno esté dentro del alcance escolar del usuario (`IScopeRepository`).

## Configuración

`ConnectionStrings:ReportsDb` en `appsettings.Development.json` (o `REPORTS_DB_CONNECTION` por variable de entorno). Corre por defecto en el puerto configurado en `Properties/launchSettings.json`.

## Desarrollo

```bash
dotnet restore
dotnet run
```

Migraciones EF Core (crean/actualizan el esquema, incluyendo tablas TEA/CIE):

```bash
dotnet ef database update
```
