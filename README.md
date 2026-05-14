# Back-end-Clinical — Microservicio de Evaluaciones Clínicas USEBEQ

[![Rust](https://img.shields.io/badge/Rust-2021-dea584?style=flat&logo=rust)](https://www.rust-lang.org/)
[![Axum](https://img.shields.io/badge/Axum-0.7-5C2D91?style=flat&logo=rust)](https://github.com/tokio-rs/axum)
[![SQLx](https://img.shields.io/badge/SQLx-0.8-4169E1?style=flat&logo=postgresql)](https://github.com/launchbadge/sqlx)
[![Supabase](https://img.shields.io/badge/Supabase-3ECF8E?style=flat&logo=supabase)](https://supabase.com/)

Microservicio backend para la gestión de **evaluaciones clínicas psicológicas** dentro del ecosistema SAE de USEBEQ. Implementa tamizajes del **Trastorno del Espectro Autista (TEA)** y provee la infraestructura base para evaluaciones **CIE (Clasificación Internacional de Enfermedades)**.

---

## 📋 Tabla de Contenidos

- [Arquitectura](#-arquitectura)
- [Stack Tecnológico](#-stack-tecnológico)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Modelos de Datos](#-modelos-de-datos)
- [API Endpoints](#-api-endpoints)
- [Flujo de Datos](#-flujo-de-datos)
- [Seguridad](#-seguridad)
- [Configuración](#-configuración)
- [Despliegue](#-despliegue)
- [Desarrollo](#-desarrollo)
- [Roadmap](#-roadmap)

---

## 🏗️ Arquitectura

```
                    ┌─────────────────────┐
                    │   Angular Frontend   │
                    └─────────┬───────────┘
                              │ HTTPS
                    ┌─────────▼───────────┐
                    │    API Gateway       │
                    │  (Auth, Rate-limit)  │
                    └─────────┬───────────┘
                              │ /api/clinical/*
                    ┌─────────▼───────────┐
                    │   Axum HTTP Server   │
                    │     (Puerto 5005)    │
                    └─────────┬───────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                     │
  ┌──────▼──────┐    ┌───────▼───────┐   ┌────────▼─────────┐
  │   JWT Auth  │    │    Health     │   │   TEA Handler     │
  │  (Extractor)│    │   Endpoint    │   │                   │
  └──────┬──────┘    └───────┬───────┘   └────────┬─────────┘
         │                   │                     │
         │            ┌──────▼──────┐     ┌───────▼──────────┐
         │            │ DB SELECT 1 │     │ TeaScoringService│
         │            └──────┬──────┘     │  (cálculo puro)  │
         │                   │            └───────┬──────────┘
         │            ┌──────▼────────────────────▼──────────┐
         │            │      PostgreSQL (Supabase)            │
         │            │  tea_screenings / tea_respuestas      │
         │            │  cie_evaluaciones / cie_respuestas    │
         │            └───────────────────────────────────────┘
         │
    ┌────▼────┐
    │JWT Auth │
    │ Servicio│
    │(externo) │
    └─────────┘
```

La aplicación sigue un **patrón de capas simplificado** con módulos funcionales:

| Capa | Responsabilidad |
|------|----------------|
| **HTTP Layer** | Router Axum + CORS + Handlers |
| **Auth Layer** | Extractor JWT (`FromRequestParts`) |
| **Service Layer** | Lógica de negocio pura (scoring) |
| **Data Layer** | Consultas SQLx directas a PostgreSQL |

No se utiliza un contenedor de DI — Axum provee `State` compartido vía `Arc<AppState>`.

---

## 🛠️ Stack Tecnológico

| Categoría | Tecnología | Versión |
|-----------|-----------|---------|
| **Lenguaje** | Rust | 2021 Edition |
| **Framework Web** | Axum | 0.7 |
| **Runtime Asíncrono** | Tokio | 1.0 (full features) |
| **Base de Datos** | PostgreSQL (Supabase) | — |
| **Driver/ORM** | SQLx | 0.8 |
| **Autenticación** | JWT (jsonwebtoken) | 9.3 |
| **Serialización** | Serde / Serde JSON | 1.0 |
| **Validación** | Validator | 0.18 (derive) |
| **Logging/Tracing** | Tracing + Tracing Subscriber | 0.1 / 0.3 |
| **UUID** | uuid | 1.0 (v4, serde) |
| **Fecha/Hora** | Chrono | 0.4 (serde) |
| **CORS** | tower-http | 0.5 |
| **Extras** | axum-extra | 0.9 (typed-header) |
| **Async Traits** | async-trait | 0.1 |
| **Variables de Entorno** | dotenvy | 0.15 |

---

## 📁 Estructura del Proyecto

```
src/
├── main.rs                       # Punto de entrada: server, routes, CORS, startup
│
├── handlers/                     # Controladores HTTP
│   ├── mod.rs
│   ├── health_check.rs           # GET /api/clinical/health
│   └── tea_handler.rs            # POST/GET evaluaciones TEA
│
├── middleware/                    # Pipeline de procesamiento
│   ├── mod.rs
│   └── auth.rs                   # Extractor JWT (FromRequestParts)
│
├── models/                       # Estructuras de datos
│   ├── mod.rs
│   ├── auth.rs                   # Claims JWT, AuthPayload
│   ├── clinical.rs               # Row structs (TeaScreening, etc.)
│   └── response.rs               # Envelope ApiResponse<T>
│
└── services/                     # Lógica de negocio
    ├── mod.rs
    ├── tea_scoring.rs            # Motor de puntuación TEA
    └── cie_scoring.rs            # Placeholder para CIE
```

**Archivos de configuración:**

```
├── Cargo.toml                    # Manifiesto del proyecto
├── .env                          # Variables de entorno (local)
├── .gitignore                    # Ignora .env, target/, Cargo.lock
└── README.md                     # Este archivo
```

---

## 🗄️ Modelos de Datos

### tea_screenings

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | `i32` (PK) | Identificador único |
| `alumno_id` | `Uuid` | Referencia al alumno evaluado |
| `evaluador_id` | `Uuid` | Referencia al evaluador (docente/psicólogo) |
| `ciclo_id` | `i32` | Ciclo escolar |
| `fecha` | `NaiveDate` | Fecha de la evaluación |
| `contexto_obs` | `Option<String>` | Observación de contexto |
| `observaciones_generales` | `Option<String>` | Observaciones adicionales |
| `puntaje_total` | `Option<i16>` | Puntaje total calculado |
| `nivel_alerta` | `Option<String>` | Nivel de alerta (Bajo/Medio/Alto) |
| `requiere_canalizacion` | `bool` | Requiere derivación a especialista |
| `created_at` | `DateTime<Utc>` | Fecha de creación |

### tea_respuestas

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | `i32` (PK) | Identificador único |
| `screening_id` | `i32` (FK) | Referencia al screening (`tea_screenings.id`) |
| `indicador_id` | `i32` | Indicador / pregunta |
| `frecuencia` | `i16` | Frecuencia observada |
| `intensidad` | `i16` | Intensidad observada |
| `observacion` | `Option<String>` | Observación adicional |

### cie_evaluaciones

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | `i32` (PK) | Identificador único |
| `alumno_id` | `Uuid` | Referencia al alumno |
| `evaluador_id` | `Uuid` | Referencia al evaluador |
| `ciclo_id` | `i32` | Ciclo escolar |
| `dimension_id` | `i32` | Dimensión evaluada |
| `fecha` | `NaiveDate` | Fecha de evaluación |
| `observaciones` | `Option<String>` | Observaciones |
| `estado` | `String` | Estado de la evaluación |
| `created_at` | `DateTime<Utc>` | Fecha de creación |

### cie_respuestas

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | `i32` (PK) | Identificador único |
| `evaluacion_id` | `i32` (FK) | Referencia a la evaluación (`cie_evaluaciones.id`) |
| `subindicador_id` | `i32` | Subindicador evaluado |
| `logrado` | `Option<bool>` | Indicador de logro |
| `nivel_ayuda` | `Option<i16>` | Nivel de ayuda requerido |
| `respuesta_tipo` | `Option<String>` | Tipo de respuesta |
| `observacion` | `Option<String>` | Observación |

### Relaciones

```
tea_screenings  1 ──── N  tea_respuestas
cie_evaluaciones 1 ──── N  cie_respuestas
```

`alumno_id` y `evaluador_id` son claves foráneas a tablas de usuarios/estudiantes gestionadas por otros microservicios del ecosistema SAE.

---

## 🌐 API Endpoints

### `GET /api/clinical/health`

Verifica el estado del servicio y la conexión a la base de datos.

**Auth:** No requiere

**Respuesta exitosa:**
```json
{
  "status_code": 200,
  "int_op_code": "CL_200",
  "data": {
    "message": "Clinical Intelligence MS is healthy. DB: Connected"
  }
}
```

**Respuesta error (DB caída):** `503 Service Unavailable`

---

### `POST /api/clinical/evaluate/tea`

Registra una evaluación TEA y devuelve el puntaje con nivel de alerta.

**Auth:** Bearer JWT (requiere `sub` como UUID válido)

**Request body:**
```json
{
  "student_id": "550e8400-e29b-41d4-a716-446655440000",
  "ciclo_id": 2025,
  "answers": [1, 2, 0, 3, 1, 2, 1, 0],
  "contexto_obs": "Observación opcional del contexto"
}
```

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `student_id` | `Uuid` | ✅ | ID del alumno evaluado |
| `ciclo_id` | `i32` | ✅ | Ciclo escolar |
| `answers` | `Vec<i32>` | ✅ | Arreglo de puntuaciones (frecuencia/intensidad) |
| `contexto_obs` | `Option<String>` | ❌ | Observación contextual |

**Respuesta exitosa:**
```json
{
  "status_code": 200,
  "int_op_code": "CL_200",
  "data": {
    "total_score": 10,
    "alert_level": "Alto"
  }
}
```

**Códigos de error:**
| Código | Significado |
|--------|-------------|
| `400` | `sub` del token no es un UUID válido |
| `401` | Token JWT ausente, inválido o expirado |
| `500` | Error al guardar en base de datos |

---

### `GET /api/clinical/history/tea`

Obtiene el historial completo de evaluaciones TEA de un alumno.

**Auth:** Bearer JWT

**Query params:**
```
?student_id=550e8400-e29b-41d4-a716-446655440000
```

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `student_id` | `Uuid` | ✅ | ID del alumno |

**Respuesta exitosa:**
```json
{
  "status_code": 200,
  "int_op_code": "CL_200",
  "data": [
    {
      "id": 1,
      "alumno_id": "550e8400-...",
      "evaluador_id": "660e8400-...",
      "ciclo_id": 2025,
      "fecha": "2025-03-15",
      "contexto_obs": null,
      "observaciones_generales": null,
      "puntaje_total": 10,
      "nivel_alerta": "Alto",
      "requiere_canalizacion": true,
      "created_at": "2025-03-15T14:30:00Z"
    }
  ]
}
```

**Códigos de error:**
| Código | Significado |
|--------|-------------|
| `401` | Token JWT ausente, inválido o expirado |
| `500` | Error al consultar base de datos |

---

## 🔄 Flujo de Datos

### Evaluación TEA

```
1. Frontend (Angular)
   ↓  POST /api/clinical/evaluate/tea  { student_id, ciclo_id, answers }
2. API Gateway
   ↓  Reenvía a microservicio
3. Axum Router
   ↓  Enruta a evaluate_tea handler
4. JWT Extractor (auth.rs)
   ↓  Valida token, extrae Claims (sub = evaluador_id)
5. TeaScoringService::calculate_score()
   ↓  Suma respuestas, determina nivel de alerta
6. SQLx INSERT → tea_screenings
   ↓  Persiste evaluación
7. ApiResponse<TeaScoringResult> → Frontend
```

### Convención de Respuesta

Todas las respuestas utilizan el envelope `ApiResponse<T>`:

```rust
pub struct ApiResponse<T> {
    pub status_code: u16,       // Código HTTP
    pub int_op_code: String,    // Código interno: "CL_{status}"
    pub data: T,                // Payload genérico
}
```

El prefijo `CL_` en `int_op_code` permite al frontend realizar búsquedas i18n de mensajes.

---

## 🔒 Seguridad

| Mecanismo | Implementación |
|-----------|---------------|
| **Autenticación JWT** | Extractor `FromRequestParts` valida Bearer token en cada ruta protegida |
| **Secreto compartido** | `JWT_SECRET` desde variable de entorno |
| **Validación de token** | `jsonwebtoken::decode` con validación default (incluye `exp`) |
| **Claims extraídos** | `sub` (user_id), `role`, `student_id` disponibles en handlers |
| **CORS** | Permisivo (`AllowOrigin::any`) — las restricciones reales se aplican en el API Gateway |
| **SQL Injection** | Prevenido mediante consultas parametrizadas (`$1`, `$2`, ...) de SQLx |
| **Secrets** | No hay secretos hardcodeados; todo vía variables de entorno |

### Consideraciones de seguridad pendientes

- ❌ No hay rate limiting
- ❌ No hay validación semántica de inputs (rangos, longitud de `answers`)
- ❌ No hay paginación en el endpoint de historial
- ❌ No hay tracing de request IDs
- ❌ No hay validación compile-time de SQL (consultas en runtime)

---

## ⚙️ Configuración

### Variables de Entorno

| Variable | Requerida | Default | Descripción |
|----------|-----------|---------|-------------|
| `DATABASE_URL` | ✅ | — | Cadena de conexión PostgreSQL (Supabase) |
| `JWT_SECRET` | ✅ | — | Secreto compartido para validación JWT |
| `PORT` | ❌ | `5005` | Puerto del servidor |
| `RUST_LOG` | ❌ | `back_end_clinical=debug` | Nivel de logging/tracing |

### Ejemplo `.env`

```env
DATABASE_URL=postgres://user:password@host:5432/database
PORT=5005
RUST_LOG=back_end_clinical=debug
JWT_SECRET=supersecretkey
```

---

## 🚀 Despliegue

### Prerrequisitos

- Rust 1.75+
- PostgreSQL 15+ (o una instancia de Supabase)

### Compilación

```bash
# Build de producción
cargo build --release

# El binario se genera en:
./target/release/back-end-clinical
```

### Ejecución

```bash
# Desarrollo
cargo run

# Producción
RUST_LOG=info ./target/release/back-end-clinical
```

### Docker (recomendado)

```dockerfile
FROM rust:1.75-slim AS builder
WORKDIR /app
COPY . .
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=builder /app/target/release/back-end-clinical /app/
COPY .env /app/
EXPOSE 5005
CMD ["/app/back-end-clinical"]
```

---

## 💻 Desarrollo

### Comandos útiles

```bash
cargo build              # Compilar
cargo run                # Ejecutar en desarrollo
cargo check              # Verificar compilación (más rápido)
cargo clippy             # Linter + mejores prácticas
cargo fmt                # Formatear código
cargo test               # Ejecutar tests
```

### Pruebas

Actualmente el proyecto **no cuenta con pruebas unitarias ni de integración**. Se recomienda agregar:

- Tests unitarios para `TeaScoringService::calculate_score()`
- Tests de integración con SQLx (in-memory o testcontainers)
- Tests de API con `axum-test` o `reqwest`

### Convenciones

- **Idioma:** Código y comentarios en español
- **Códigos de operación:** Prefijo `CL_` seguido del código HTTP
- **Nomenclatura:** `snake_case` para Rust, `snake_case` para columnas DB
- **Errores:** Manejo inline con `match` + `tracing::error!()` + `ApiResponse` con código HTTP apropiado

---

## 🗺️ Roadmap

### Implementado ✅

- [x] Health check con verificación de base de datos
- [x] Evaluación TEA (tamizaje de autismo)
- [x] Historial de evaluaciones TEA por alumno
- [x] Autenticación JWT con extracción de claims
- [x] Envelope de respuesta estandarizado
- [x] Arquitectura modular (handlers / services / models / middleware)

### En progreso 🔄

- [ ] Persistencia de respuestas detalladas (`tea_respuestas`)
- [ ] Motor de puntuación CIE (Clasificación Internacional de Enfermedades)

### Planificado 📋

- [ ] Validación de datos (rangos, longitudes) usando `validator`
- [ ] Paginación en endpoint de historial
- [ ] Tests unitarios y de integración
- [ ] Rate limiting (tower middleware)
- [ ] Request ID tracing
- [ ] Migraciones SQL con SQLx CLI
- [ ] Documentación OpenAPI/Swagger (utoipa)

---

## 📄 Licencia

Propietario — USEBEQ. Todos los derechos reservados.

---

## 👥 Equipo

- **USEBEQ** — Unidad de Servicios para la Educación Básica en el Estado de Querétaro
- **Ecosistema SAE** — Sistema de Apoyo Educativo

---

> Documentación generada a partir del análisis estático del código fuente.
