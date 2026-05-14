# PLAN DE ARQUITECTURA: AGENTE ESPECIALISTA TEA CON OLLAMA EN LA NUBE

> Documento técnico para la implementación de un Agente de Inteligencia Artificial
> especializado en análisis clínico de tamizajes TEA (Trastorno del Espectro Autista),
> conectado al microservicio Back-end-Clinical de USEBEQ.

---

## 📋 Índice

1. [Visión General](#1-visión-general)
2. [Arquitectura del Sistema](#2-arquitectura-del-sistema)
3. [Componentes del Agente Especialista](#3-componentes-del-agente-especialista)
4. [Integración con el Microservicio Rust](#4-integración-con-el-microservicio-rust)
5. [Ollama en la Nube: Infraestructura](#5-ollama-en-la-nube-infraestructura)
6. [Flujo Clínico Completo](#6-flujo-clínico-completo)
7. [Modelo de Datos Extendido](#7-modelo-de-datos-extendido)
8. [API del Agente Especialista](#8-api-del-agente-especialista)
9. [Prompt Engineering Clínico](#9-prompt-engineering-clínico)
10. [Seguridad y Privacidad](#10-seguridad-y-privacidad)
11. [Plan de Implementación](#11-plan-de-implementación)
12. [Métricas y Evaluación](#12-métricas-y-evaluación)
13. [Presupuesto de Recursos Nube](#13-presupuesto-de-recursos-nube)

---

## 1. VISIÓN GENERAL

### 1.1 ¿Qué es el Agente Especialista TEA?

Es un **sistema de inteligencia artificial clínica** que actúa como un psicólogo especialista
en autismo. Analiza las respuestas de tamizaje TEA y produce:

- **Perfil de severidad multidimensional** (no solo una puntuación global)
- **Análisis por dominios clínicos** (comunicación social, interacción, comportamientos repetitivos)
- **Recomendaciones personalizadas** para intervención educativa
- **Detección de patrones de riesgo** (banderas rojas)
- **Sugerencia de canalización** a especialistas externos
- **Reporte narrativo** en lenguaje natural para docentes y padres

### 1.2 Diferenciación del Scoring Actual

| Aspecto | Scoring Actual (Rust puro) | Agente Especialista (Ollama + Rust) |
|---------|---------------------------|--------------------------------------|
| **Cálculo** | Suma lineal de puntos | Análisis multidimensional contextual |
| **Niveles** | 3 niveles (Bajo/Medio/Alto) | Perfil continuo con matices por dominio |
| **Recomendaciones** | Ninguna | Recomendaciones personalizadas |
| **Lenguaje natural** | No | Reporte narrativo completo |
| **Patrones complejos** | No detecta | Detecta correlaciones entre indicadores |
| **Contexto pedagógico** | No considerado | Integra observaciones contextuales |

---

## 2. ARQUITECTURA DEL SISTEMA

```
                            ┌─────────────────────────────────────┐
                            │           INTERNET / VPN             │
                            └─────────────────────────────────────┘
                                      │               │
            ┌─────────────────────────┘               └─────────────────────────┐
            │                                                                   │
    ┌───────▼───────────────────┐                                   ┌──────────▼──────────────────┐
    │   ECOSISTEMA USEBEQ SAE   │                                   │   INFRAESTRUCTURA NUBE       │
    │                           │                                   │                              │
    │  ┌─────────────────────┐  │                                   │  ┌────────────────────────┐  │
    │  │   Angular Frontend  │  │                                   │  │   Ollama Server (GPU) │  │
    │  └─────────┬───────────┘  │                                   │  │                        │  │
    │            │              │                                   │  │  ┌──────────────────┐  │  │
    │  ┌─────────▼───────────┐  │                                   │  │  │ Llama 3 70B      │  │  │
    │  │   API Gateway        │  │                                   │  │  │ Mistral 7B       │  │  │
    │  │   (Auth Centralizado)│  │                                   │  │  │ (fine-tuned TEA) │  │  │
    │  └─────────┬───────────┘  │                                   │  │  └──────────────────┘  │  │
    │            │              │                                   │  └────────────────────────┘  │
    │  ┌─────────▼───────────┐  │                                   │             │                │
    │  │  Back-end-Clinical  │  │                                   │  ┌──────────▼────────────┐  │
    │  │  (Rust/Axum/Puerto  │  │                                   │  │   Agente Especialista │  │
    │  │        5005)        │  │                                   │  │   (Python FastAPI)    │  │
    │  │                     │  │                                   │  │                        │  │
    │  │  ┌───────────────┐  │  │                                   │  │  ┌──────────────────┐  │  │
    │  │  │ TEA Handler   │──┼─┼─┼─ HTTP POST /api/clinical/───────┼──┼─►│ /analyze/tea     │  │  │
    │  │  │ (Rust)        │  │  │                                   │  │  │ /report/{id}     │  │  │
    │  │  └───────┬───────┘  │  │                                   │  │  │ /recommend/{id} │  │  │
    │  │          │          │  │                                   │  │  └──────────────────┘  │  │
    │  │  ┌───────▼───────┐  │  │                                   │  └────────────────────────┘  │
    │  │  │ PostgreSQL    │  │  │                                   │                              │
    │  │  │ (Supabase)    │  │  │                                   │   Recursos cloud:            │
    │  │  └───────────────┘  │  │                                   │   - 1 GPU NVIDIA A10G (24GB) │
    │  └─────────────────────┘  │                                   │   - 8 vCPU / 32GB RAM        │
    │                           │                                   │   - 100GB SSD                │
    └───────────────────────────┘                                   └──────────────────────────────┘
```

### 2.1 Flujo de Comunicación

```
[Frontend Angular]
       │
       ▼
[API Gateway] ─── autenticación JWT ───► [Back-end-Clinical (Rust)]
       │                                          │
       │                              ┌───────────┴───────────┐
       │                              │                       │
       │                      ┌───────▼───────┐       ┌──────▼──────┐
       │                      │   PostgreSQL  │       │   Ollama    │
       │                      │   (Supabase)  │       │   (Cloud)   │
       │                      └───────────────┘       └─────────────┘
       │                                                                 
  [Respuesta JSON unificada]
```

**La comunicación entre Rust y Ollama es:**
- **Síncrona** para el análisis inmediato POST /evaluate/tea
- **Asíncrona** (opcional) para reportes detallados GET /report/{id}

---

## 3. COMPONENTES DEL AGENTE ESPECIALISTA

### 3.1 Módulo de Análisis Clínico (Python FastAPI)

Servicio independiente en la nube que orquesta la interacción con Ollama.

```
agente-especialista/
├── main.py                    # FastAPI entry point
├── requirements.txt           # Dependencias Python
├── Dockerfile                 # Container image
├── config/
│   ├── settings.py            # Configuración global
│   ├── prompts/
│   │   ├── base_tea.yaml      # Prompt base para análisis TEA
│   │   ├── domains.yaml       # Definición de dominios clínicos
│   │   └── recommendations.yaml # Plantillas de recomendaciones
│   └── models/
│       └── clinical_rules.yaml # Reglas clínicas complementarias
├── api/
│   ├── __init__.py
│   ├── routes.py              # Endpoints del agente
│   └── schemas.py             # Pydantic models
├── core/
│   ├── __init__.py
│   ├── ollama_client.py       # Cliente HTTP para Ollama API
│   ├── tea_analyzer.py        # Orquestador de análisis TEA
│   ├── prompt_builder.py      # Constructor de prompts clínicos
│   └── response_parser.py     # Parseo de respuestas del LLM
├── services/
│   ├── __init__.py
│   ├── clinical_service.py    # Lógica de negocio clínica
│   └── report_service.py      # Generación de reportes
├── utils/
│   ├── __init__.py
│   ├── validators.py          # Validación clínica de datos
│   └── security.py            # Firma HMAC entre servicios
└── tests/
    ├── test_analyzer.py
    ├── test_prompts.py
    └── test_integration.py
```

### 3.2 Motor de Inferencia (Ollama + Modelo LLM)

**Modelo recomendado:** `llama3-tea-clinical` (fine-tuned)
- **Base:** Llama 3 70B o Mistral 7B
- **Fine-tuning:** Instrucciones clínicas para tamizaje TEA basado en DSM-5
- **Cuantización:** Q4_K_M (balance calidad/rendimiento)

**Estructura del fine-tuning:**
```
dataset/
├── train/
│   ├── tea_cases_1000.jsonl       # 1000 casos clínicos sintéticos
│   ├── tea_diagnosis_500.jsonl    # 500 diagnósticos validados
│   └── recommendations_800.jsonl  # 800 pares recomendación-contexto
├── validation/
│   └── tea_validation_200.jsonl   # 200 casos de validación
└── schema.md                      # Esquema de datos clínicos
```

### 3.3 Módulo de Reglas Clínicas (Complemento deterministico)

Ejecutado **antes** de la consulta al LLM para:

1. **Validar respuestas** (rangos, consistencia interna)
2. **Calcular métricas base** (puntuación por dominio)
3. **Detectar inconsistencias** (patrones contradictorios)
4. **Enriquecer el prompt** con métricas calculadas

```python
# Ejemplo de reglas clínicas
RULES = {
    "dominio_social": {
        "indicadores": [1, 2, 3, 4, 5],  # IDs de indicadores
        "pesos": [0.3, 0.2, 0.2, 0.15, 0.15],
        "umbral_alto": 8,
        "umbral_medio": 4
    },
    "dominio_comunicacion": {
        "indicadores": [6, 7, 8, 9, 10],
        "pesos": [0.25, 0.25, 0.2, 0.15, 0.15],
        "umbral_alto": 7,
        "umbral_medio": 3
    },
    "banderas_rojas": {
        "autolesion": {"indicador": 15, "accion": "canalizacion_inmediata"},
        "regresion_lenguaje": {"indicador": 22, "accion": "evaluacion_neurologica"}
    }
}
```

---

## 4. INTEGRACIÓN CON EL MICROSERVICIO RUST

### 4.1 Nuevo Módulo en Rust: `agent_client`

Se crea un nuevo módulo dentro del microservicio existente para comunicarse con el Agente Especialista.

```
src/
├── main.rs                       # Modificado: nueva ruta + state
├── handlers/
│   ├── mod.rs
│   ├── health_check.rs
│   ├── tea_handler.rs            # Modificado: llama al agente
│   └── agent_handler.rs          # NUEVO: endpoints del agente
├── middleware/
│   ├── mod.rs
│   └── auth.rs
├── models/
│   ├── mod.rs
│   ├── auth.rs
│   ├── clinical.rs
│   ├── response.rs
│   └── agent.rs                  # NUEVO: DTOs del agente
├── services/
│   ├── mod.rs
│   ├── tea_scoring.rs
│   ├── cie_scoring.rs
│   └── agent_client.rs           # NUEVO: cliente HTTP para el agente
└── config/
    └── settings.rs               # NUEVO: configuración centralizada
```

### 4.2 DTOs de Comunicación (Rust ↔ Agente Python)

**Request desde Rust al Agente:**
```rust
// src/models/agent.rs
#[derive(Debug, Serialize, Deserialize)]
pub struct AgentAnalysisRequest {
    pub screening_id: i32,
    pub student_id: Uuid,
    pub evaluator_id: Uuid,
    pub ciclo_id: i32,
    pub answers: Vec<i32>,           // Respuestas del tamizaje
    pub dominio_scores: DominioScores, // Puntuaciones precalculadas
    pub contexto_obs: Option<String>,
    pub antecedentes: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct DominioScores {
    pub social: f64,
    pub comunicacion: f64,
    pub comportamientos_repetitivos: f64,
    pub sensorial: f64,
    pub total: f64,
}
```

**Response desde el Agente a Rust:**
```rust
#[derive(Debug, Serialize, Deserialize)]
pub struct AgentAnalysisResponse {
    pub analysis_id: Uuid,
    pub screening_id: i32,
    
    // Análisis clínico
    pub perfil_severidad: PerfilSeveridad,
    pub dominios_afectados: Vec<DominioAnalisis>,
    pub patrones_detectados: Vec<PatronClinico>,
    pub banderas_rojas: Vec<BanderaRoja>,
    
    // Recomendaciones
    pub nivel_intervencion: String,
    pub recomendaciones: Vec<Recomendacion>,
    pub requiere_canalizacion: bool,
    pub especialista_sugerido: Option<String>,
    
    // Reporte narrativo
    pub resumen_clinico: String,
    pub reporte_docente: String,
    pub reporte_familia: String,
    
    // Metadatos
    pub confianza_analisis: f64,
    pub modelo_utilizado: String,
    pub tiempo_procesamiento_ms: u64,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct PerfilSeveridad {
    pub nivel_global: String,       // "Leve", "Moderado", "Severo"
    pub puntaje_estandarizado: f64,
    pub percentil: f64,
    pub interpretacion: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct DominioAnalisis {
    pub nombre: String,              // "Interacción Social"
    pub puntaje: f64,
    pub nivel: String,               // "Sin afectación" / "Leve" / "Moderado" / "Severo"
    pub descripcion: String,
    pub recomendaciones_especificas: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct PatronClinico {
    pub tipo: String,                // "regresion", "aislamiento", "rigidez"
    pub severidad: String,
    pub evidencia: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct BanderaRoja {
    pub indicador: String,
    pub severidad: String,           // "Alerta", "Urgente", "Crítico"
    pub accion_recomendada: String,
    pub plazo_accion: String,        // "24 horas", "7 días", "30 días"
}

#[derive(Debug, Serialize, Deserialize)]
pub struct Recomendacion {
    pub area: String,
    pub prioridad: String,           // "Alta", "Media", "Baja"
    pub descripcion: String,
    pub sugerencia_intervencion: String,
}
```

### 4.3 Cliente HTTP Rust para el Agente

```rust
// src/services/agent_client.rs
use reqwest::Client;  // NOTA: agregar reqwest a Cargo.toml
use crate::models::agent::*;
use std::time::Duration;

#[derive(Clone)]
pub struct AgentClient {
    http_client: Client,
    base_url: String,
    api_key: String,
    timeout_seconds: u64,
}

impl AgentClient {
    pub fn new() -> Self {
        Self {
            http_client: Client::builder()
                .timeout(Duration::from_secs(120))  // LLM puede tomar tiempo
                .build()
                .expect("Failed to create HTTP client"),
            base_url: std::env::var("AGENT_BASE_URL")
                .expect("AGENT_BASE_URL must be set"),
            api_key: std::env::var("AGENT_API_KEY")
                .expect("AGENT_API_KEY must be set"),
            timeout_seconds: std::env::var("AGENT_TIMEOUT")
                .unwrap_or_else(|_| "120".to_string())
                .parse()
                .unwrap_or(120),
        }
    }

    pub async fn analyze_tea(
        &self,
        request: &AgentAnalysisRequest,
    ) -> Result<AgentAnalysisResponse, AgentError> {
        let response = self.http_client
            .post(format!("{}/api/v1/analyze/tea", self.base_url))
            .header("X-API-Key", &self.api_key)
            .header("X-Service-Name", "back-end-clinical")
            .json(request)
            .send()
            .await
            .map_err(|e| AgentError::ConnectionError(e.to_string()))?;

        if !response.status().is_success() {
            return Err(AgentError::AgentError(
                response.status().as_u16(),
                response.text().await.unwrap_or_default(),
            ));
        }

        response
            .json::<AgentAnalysisResponse>()
            .await
            .map_err(|e| AgentError::DeserializationError(e.to_string()))
    }
}

#[derive(Debug)]
pub enum AgentError {
    ConnectionError(String),
    AgentError(u16, String),
    DeserializationError(String),
    TimeoutError,
}
```

### 4.4 Modificaciones a `main.rs`

Se agregan las nuevas dependencias al `AppState`:

```rust
// src/main.rs (modificado)
pub struct AppState {
    pub db: sqlx::PgPool,
    pub agent_client: AgentClient,  // NUEVO
}
```

Y las nuevas rutas:

```rust
// Rutas del agente especialista
.route("/api/clinical/agent/analyze/tea", post(handlers::agent_handler::analyze_with_agent))
.route("/api/clinical/agent/report/{analysis_id}", get(handlers::agent_handler::get_agent_report))
.route("/api/clinical/agent/status", get(handlers::agent_handler::agent_health))
```

### 4.5 Nuevas Variables de Entorno

```env
# Configuración del Agente Especialista
AGENT_BASE_URL=https://agente-tea.usebeq.cloud/api/v1
AGENT_API_KEY=sk-tea-clinical-xxxxxxxxxxxx
AGENT_TIMEOUT=120
AGENT_MODEL=llama3-tea-clinical:70b-q4
```

---

## 5. OLLAMA EN LA NUBE: INFRAESTRUCTURA

### 5.1 Proveedor de Nube Recomendado

**Opción 1: AWS EC2 G5 (GPU)**
- Instancia: `g5.xlarge` (1x NVIDIA A10G, 24GB VRAM)
- Costo: ~$1.006/hr (bajo demanda) | ~$0.36/hr (reservada 1 año)
- Región: us-east-1 (cerca de Supabase)
- AMI: Deep Learning AMI + Docker

**Opción 2: Google Cloud G2**
- Instancia: `g2-standard-4` (1x NVIDIA L4, 24GB VRAM)
- Costo: ~$0.85/hr

**Opción 3: Vultr Cloud GPU**
- Instancia: `GPU-2C-8GB` (1x GPU compartida)
- Costo: ~$0.79/hr

### 5.2 Stack de Infraestructura Cloud

```
                    ┌──────────────────────────────┐
                    │     Load Balancer (ALB/NLB)   │
                    │     HTTPS / TLS termination   │
                    └──────────────┬───────────────┘
                                   │
                    ┌──────────────▼───────────────┐
                    │   Docker Compose / Kubernetes │
                    │                              │
                    │  ┌────────────────────────┐  │
                    │  │  Nginx Reverse Proxy   │  │
                    │  │  (rate limit, auth)    │  │
                    │  └───────────┬────────────┘  │
                    │              │                │
                    │  ┌───────────▼────────────┐  │
                    │  │ FastAPI (Agente)       │  │
                    │  │ - 4 workers (gunicorn) │  │
                    │  └───────────┬────────────┘  │
                    │              │                │
                    │  ┌───────────▼────────────┐  │
                    │  │ Ollama Server          │  │
                    │  │ - Modelo: llama3-tea   │  │
                    │  │ - GPU: NVIDIA A10G     │  │
                    │  │ - keep_alive: 5min     │  │
                    │  └────────────────────────┘  │
                    │                              │
                    │  ┌────────────────────────┐  │
                    │  │ Redis Cache            │  │
                    │  │ - Resultados frecuentes│  │
                    │  │ - TTL: 24h             │  │
                    │  └────────────────────────┘  │
                    └──────────────────────────────┘
                                   │
                    ┌──────────────▼───────────────┐
                    │     CloudWatch / Loki        │
                    │     (logs + métricas)        │
                    └──────────────────────────────┘
```

### 5.3 Docker Compose para el Agente

```yaml
# docker-compose.cloud.yml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    ports:
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - agent-api

  agent-api:
    build: .
    environment:
      - OLLAMA_HOST=http://ollama:11434
      - REDIS_URL=redis://redis:6379
      - MODEL_NAME=llama3-tea-clinical:70b-q4
      - LOG_LEVEL=INFO
    ports:
      - "8000:8000"
    depends_on:
      - ollama
      - redis
    deploy:
      resources:
        reservations:
          cpus: '4'
          memory: 8G

  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
    volumes:
      - ollama_models:/root/.ollama
      - ./custom_models:/custom_models
    environment:
      - OLLAMA_KEEP_ALIVE=300s
      - OLLAMA_NUM_PARALLEL=2
      - OLLAMA_MAX_QUEUE=8
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  ollama_models:
  redis_data:
```

### 5.4 Script de Despliegue Automatizado

```bash
#!/bin/bash
# deploy_agent.sh — Despliegue del Agente Especialista en Cloud

set -euo pipefail

INSTANCE_IP=$1
SSH_KEY=$2

echo "🚀 Desplegando Agente Especialista TEA en $INSTANCE_IP"

# 1. SCP archivos al servidor
scp -i $SSH_KEY -r ./agente-especialista/ ubuntu@$INSTANCE_IP:/home/ubuntu/
scp -i $SSH_KEY docker-compose.cloud.yml ubuntu@$INSTANCE_IP:/home/ubuntu/
scp -i $SSH_KEY .env.cloud ubuntu@$INSTANCE_IP:/home/ubuntu/.env

# 2. SSH: instalar dependencias del sistema
ssh -i $SSH_KEY ubuntu@$INSTANCE_IP << 'EOF'
    # Instalar NVIDIA drivers + CUDA
    sudo apt-get update
    sudo apt-get install -y nvidia-driver-535 nvidia-cuda-toolkit
    
    # Instalar Docker + NVIDIA Container Toolkit
    curl -fsSL https://get.docker.com | sudo bash
    distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
    curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
    curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list
    sudo apt-get update && sudo apt-get install -y nvidia-container-toolkit
    sudo systemctl restart docker
    
    # Verificar GPU
    sudo docker run --rm --gpus all nvidia/cuda:12.2.0-base nvidia-smi
    
    # Descargar modelo base Ollama
    sudo docker exec ollama ollama pull llama3:70b
    
    # Aplicar fine-tuning (si existe)
    if [ -f "/custom_models/llama3-tea-clinical.gguf" ]; then
        sudo docker exec ollama ollama create llama3-tea-clinical -f /custom_models/Modelfile
    fi
    
    # Iniciar stack
    sudo docker compose -f docker-compose.cloud.yml up -d
EOF

echo "✅ Agente desplegado exitosamente en https://$INSTANCE_IP"
```

---

## 6. FLUJO CLÍNICO COMPLETO

### 6.1 Flujo Síncrono: Evaluación TEA + Análisis

```
TIEMPO: 0ms                    FRONTEND                       
  │                              │                              
  │                              │ POST /api/clinical/evaluate/tea
  │                              │ { student_id, ciclo_id, answers }
  │                              ▼                              
  │                     ┌──────────────────┐                    
  │                     │  API Gateway     │                    
  │                     │  (Valida JWT)    │                    
  │                     └────────┬─────────┘                    
  │                              │                              
  │                     ┌────────▼─────────┐                    
  │                     │  tea_handler.rs   │                    
  │                     │  (Rust/Axum)      │                    
  │                     └────────┬─────────┘                    
  │                              │                              
  │                    ┌─────────▼──────────┐                   
  │                    │ 1. Validar request │                   
  │                    │ 2. Calcular scores │                   
  │                    │    por dominio     │                   
  │                    │    (Rust puro)     │                   
  │                    └─────────┬──────────┘                   
  │                              │                              
  │                    ┌─────────▼──────────┐                   
  │                    │ 3. Guardar en DB   │                   
  │                    │    tea_screenings  │                   
  │                    │    tea_respuestas  │                   
  │                    └─────────┬──────────┘                   
  │                              │                              
  │                    ┌─────────▼──────────┐                   
  │                    │ 4. Llamar Agente   │◄─────── NUEVO ────┐
  │                    │    HTTP POST       │                   │
  │                    │    /api/v1/analyze/tea                │
  │                    └─────────┬──────────┘                   │
  │                              │                             │
  │                    ┌─────────▼──────────┐                   │
  │                    │ 5. Agente Python   │                   │
  │                    │    - Construir      │                   │
  │                    │      prompt clínico│                   │
  │                    │    - Consultar      │                   │
  │                    │      Ollama (LLM)   │                   │
  │                    │    - Parsear        │                   │
  │                    │      respuesta      │                   │
  │                    │    - Validar        │                   │
  │                    │      clínicamente   │                   │
  │                    │    - Cachear en     │                   │
  │                    │      Redis          │                   │
  │                    └─────────┬──────────┘                   │
  │                              │                             │
  │                    ┌─────────▼──────────┐                   │
  │                    │ 6. Guardar análisis│                   │
  │                    │    en DB (tabla     │                   │
  │                    │    tea_analisis)    │                   │
  │                    └─────────┬──────────┘                   │
  │                              │                             │
  │                    ┌─────────▼──────────┐                   
  │                    │ 7. Response JSON   │                   
  │                    │    ApiResponse<     │                   
  │                    │     TeaResult>      │                   
  │                    └─────────┬──────────┘                   
  │                              │                              
  ▼ TIEMPO: ~3-8s           FRONTEND RECIBE                     
                           RESULTADO COMPLETO
```

### 6.2 Flujo Asíncrono: Reporte Detallado

```
FRONTEND                                    BACK-END              AGENTE
   │                                           │                    │
   │  GET /api/clinical/agent/report/{id}      │                    │
   │──────────────────────────────────────────►│                    │
   │                                           │                    │
   │                              ¿En caché?   │                    │
   │                                   │       │                    │
   │                              ┌────▼────┐  │                    │
   │                              │  Redis   │──────────────────────│
   │                              │  HIT?    │                    │
   │                              └────┬────┘                    │
   │                                   │                        │
   │                    ┌──────────────┴──────────────┐          │
   │                    │         │                   │          │
   │                    ▼         ▼                   ▼          │
   │              Devolver    Consultar DB    Llamar Agente      │
   │              cache       tea_analisis    generate_report    │
   │                    │         │                   │          │
   │                    └─────────┴───────────────────┘          │
   │                                           │                 │
   │  Response JSON con reporte completo       │                 │
   │◄──────────────────────────────────────────│                 │
```

---

## 7. MODELO DE DATOS EXTENDIDO

### 7.1 Nueva Tabla: `tea_analisis`

```sql
-- Almacena los análisis generados por el Agente Especialista
CREATE TABLE tea_analisis (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    screening_id    INTEGER NOT NULL REFERENCES tea_screenings(id) ON DELETE CASCADE,
    
    -- Perfil de severidad
    nivel_global        VARCHAR(20) NOT NULL,  -- 'Leve', 'Moderado', 'Severo'
    puntaje_estandar    DECIMAL(5,2),
    percentil           DECIMAL(5,2),
    interpretacion      TEXT,
    
    -- Dominios (JSON para flexibilidad)
    dominios_afectados  JSONB NOT NULL DEFAULT '[]',
    -- [
    --   {"nombre": "Interacción Social", "puntaje": 7.5, "nivel": "Moderado", ...},
    --   {"nombre": "Comunicación", "puntaje": 3.2, "nivel": "Leve", ...}
    -- ]
    
    -- Patrones y alertas
    patrones_detectados JSONB DEFAULT '[]',
    banderas_rojas      JSONB DEFAULT '[]',
    
    -- Recomendaciones
    nivel_intervencion      VARCHAR(30),  -- 'Temprana', 'Especializada', 'Intensiva'
    recomendaciones         JSONB NOT NULL DEFAULT '[]',
    requiere_canalizacion   BOOLEAN DEFAULT false,
    especialista_sugerido   VARCHAR(100),
    
    -- Reportes en lenguaje natural
    resumen_clinico     TEXT NOT NULL,
    reporte_docente     TEXT,
    reporte_familia     TEXT,
    
    -- Metadatos
    confianza_analisis      DECIMAL(3,2),  -- 0.00 a 1.00
    modelo_utilizado        VARCHAR(50),
    tiempo_procesamiento_ms INTEGER,
    prompt_version          VARCHAR(20),
    
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Índices
CREATE INDEX idx_tea_analisis_screening ON tea_analisis(screening_id);
CREATE INDEX idx_tea_analisis_nivel ON tea_analisis(nivel_global);
CREATE INDEX idx_tea_analisis_created ON tea_analisis(created_at DESC);

-- Trigger para updated_at
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_tea_analisis_updated
    BEFORE UPDATE ON tea_analisis
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();
```

### 7.2 Nueva Tabla: `tea_recomendaciones_historial`

```sql
CREATE TABLE tea_recomendaciones_historial (
    id              SERIAL PRIMARY KEY,
    analisis_id     UUID NOT NULL REFERENCES tea_analisis(id) ON DELETE CASCADE,
    alumno_id       UUID NOT NULL,
    recomendacion   TEXT NOT NULL,
    area            VARCHAR(50),
    prioridad       VARCHAR(10),     -- 'Alta', 'Media', 'Baja'
    estado          VARCHAR(20) DEFAULT 'pendiente', -- 'pendiente', 'en_progreso', 'completada'
    implementada_por UUID,           -- quien aplicó la recomendación
    fecha_implementacion TIMESTAMPTZ,
    resultado       TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 8. API DEL AGENTE ESPECIALISTA

### 8.1 Endpoints del Servicio Python (FastAPI)

| Método | Ruta | Descripción | Timeout |
|--------|------|-------------|---------|
| `POST` | `/api/v1/analyze/tea` | Análisis clínico completo de tamizaje TEA | 120s |
| `GET` | `/api/v1/report/{analysis_id}` | Obtener reporte por ID | 5s |
| `GET` | `/api/v1/report/{analysis_id}/docente` | Reporte en formato para docentes | 5s |
| `GET` | `/api/v1/report/{analysis_id}/familia` | Reporte en formato para familias | 5s |
| `POST` | `/api/v1/health` | Health check del agente | 5s |
| `GET` | `/api/v1/model/info` | Información del modelo cargado | 5s |
| `POST` | `/api/v1/model/swap` | Cambiar modelo activo (admin) | 30s |

### 8.2 Ejemplo de Request/Response Completo

**Request:**
```json
POST /api/v1/analyze/tea
X-API-Key: sk-tea-clinical-xxxx
X-Service-Name: back-end-clinical

{
  "screening_id": 1234,
  "student_id": "550e8400-e29b-41d4-a716-446655440000",
  "evaluator_id": "660e8400-e29b-41d4-a716-446655440001",
  "ciclo_id": 2025,
  "answers": [2, 3, 0, 1, 3, 2, 0, 1, 2, 2, 3, 1, 0, 2, 1],
  "dominio_scores": {
    "social": 8.5,
    "comunicacion": 4.2,
    "comportamientos_repetitivos": 6.1,
    "sensorial": 2.0,
    "total": 20.8
  },
  "contexto_obs": "El alumno presenta dificultades para integrarse en juegos grupales. Prefiere actividades solitarias.",
  "antecedentes": "Diagnóstico previo de retraso en el lenguaje a los 3 años."
}
```

**Response:**
```json
{
  "analysis_id": "7c9a5b3e-1a2b-3c4d-5e6f-7a8b9c0d1e2f",
  "screening_id": 1234,
  "perfil_severidad": {
    "nivel_global": "Moderado",
    "puntaje_estandarizado": 62.5,
    "percentil": 84.3,
    "interpretacion": "El puntaje total sugiere un nivel de severidad MODERADO, consistente con un perfil de Trastorno del Espectro Autista que requiere intervención especializada. Se recomienda evaluación por neurólogo pediatra."
  },
  "dominios_afectados": [
    {
      "nombre": "Interacción Social Recíproca",
      "puntaje": 8.5,
      "nivel": "Moderado-Severo",
      "descripcion": "Dificultades significativas en la reciprocidad social. El alumno raramente inicia interacciones y presenta respuestas limitadas a los intentos de sus pares.",
      "recomendaciones_especificas": [
        "Implementar programa de habilidades sociales con modelado en video",
        "Asignar un compañero tutor (peer buddy) durante actividades estructuradas",
        "Utilizar historias sociales para anticipar situaciones de interacción"
      ]
    },
    {
      "nombre": "Comunicación Social",
      "puntaje": 4.2,
      "nivel": "Leve",
      "descripcion": "Presenta algunas dificultades en la comunicación pragmática, pero mantiene capacidad de expresar necesidades básicas.",
      "recomendaciones_especificas": [
        "Fortalecer el uso de lenguaje pragmático en contextos naturales",
        "Implementar sistema de comunicación visual como apoyo"
      ]
    },
    {
      "nombre": "Comportamientos Repetitivos y Restringidos",
      "puntaje": 6.1,
      "nivel": "Moderado",
      "descripcion": "Presenta intereses restringidos intensos y adherencia a rutinas. La interrupción de rutinas genera ansiedad significativa.",
      "recomendaciones_especificas": [
        "Crear un horario visual predictible con transiciones señalizadas",
        "Trabajar flexibilidad cognitiva mediante juegos de cambio de reglas",
        "Permitir períodos de intereses especiales como refuerzo"
      ]
    },
    {
      "nombre": "Procesamiento Sensorial",
      "puntaje": 2.0,
      "nivel": "Sin afectación significativa",
      "descripcion": "No se detectan alteraciones sensoriales significativas en el tamizaje.",
      "recomendaciones_especificas": []
    }
  ],
  "patrones_detectados": [
    {
      "tipo": "Evitación social activa",
      "severidad": "Moderada",
      "evidencia": [
        "Puntuación alta en indicadores de retraimiento social",
        "Contexto observacional confirma aislamiento en recreos",
        "No responde a iniciativas de pares"
      ]
    },
    {
      "tipo": "Adherencia rígida a rutinas",
      "severidad": "Moderada",
      "evidencia": [
        "Malestar significativo ante cambios no anticipados",
        "Preguntas repetitivas sobre horarios y secuencias"
      ]
    }
  ],
  "banderas_rojas": [
    {
      "indicador": "Regresión en habilidades previamente adquiridas",
      "severidad": "Alerta",
      "accion_recomendada": "Evaluación neurológica para descartar condiciones comórbidas",
      "plazo_accion": "7 días"
    }
  ],
  "nivel_intervencion": "Especializada",
  "recomendaciones": [
    {
      "area": "Intervención Educativa",
      "prioridad": "Alta",
      "descripcion": "Implementar adaptaciones curriculares significativas con apoyo de USAER",
      "sugerencia_intervencion": "Solicitar evaluación de USAER para determinar apoyos específicos. Considerar reducción de estímulos auditivos en el aula."
    },
    {
      "area": "Apoyo Familiar",
      "prioridad": "Alta",
      "descripcion": "Programa de psicoeducación familiar sobre TEA",
      "sugerencia_intervencion": "Referir a la Escuela para Padres de USEBEQ. Proporcionar guía de estrategias en casa."
    },
    {
      "area": "Salud",
      "prioridad": "Media",
      "descripcion": "Evaluación por neurólogo pediatra",
      "sugerencia_intervencion": "Canalizar al Centro de Salud para valoración neurológica y determinar necesidad de terapia ocupacional."
    }
  ],
  "requiere_canalizacion": true,
  "especialista_sugerido": "Neurólogo Pediatra - Terapeuta Ocupacional - Psicólogo Clínico",
  "resumen_clinico": "El tamizaje aplicado revela un perfil compatible con Trastorno del Espectro Autista de nivel MODERADO. Las áreas más afectadas son la interacción social recíproca (puntaje 8.5/12) y los comportamientos repetitivos (6.1/12). Se detectó una bandera roja por posible regresión de habilidades que requiere evaluación neurológica en los próximos 7 días. Se recomienda intervención educativa especializada con apoyo de USAER, psicoeducación familiar, y canalización a neurología pediátrica.",
  "reporte_docente": "--- INICIO REPORTE DOCENTE ---\n\nEstimado(a) docente,\n\nCon base en el tamizaje TEA aplicado al alumno, se identificó un perfil de MODERADA severidad en el espectro autista. A continuación, se presentan recomendaciones prácticas para el aula:\n\n1. **Estrategias de interacción social:**\n   - Asignar un compañero tutor para actividades grupales\n   - Utilizar historias sociales antes de eventos nuevos\n   - Proporcionar tiempo de respuesta extendido (10-15 segundos)\n\n2. **Manejo de rutinas:**\n   - Establecer un horario visual claro y predecible\n   - Anticipar cambios con 10 minutos de aviso\n   - Mantener consistencia en las transiciones\n\n3. **Comunicación:**\n   - Usar instrucciones claras y concretas\n   - Apoyar con imágenes o gestos\n   - Verificar comprensión pidiendo repetición\n\n4. **Adecuaciones:**\n   - Ubicar al alumno en zona de menor estimulación\n   - Permitir descansos sensoriales cuando sea necesario\n   - Evaluar con formatos adaptados si es requerido\n\nCualquier duda, contactar al departamento de psicología.\n\n--- FIN REPORTE DOCENTE ---",
  "reporte_familia": "--- INICIO REPORTE FAMILIA ---\n\nEstimada familia,\n\nHemos realizado una evaluación de desarrollo a su hijo(a) y queremos compartir con ustedes los resultados de manera clara y respetuosa.\n\n**¿Qué encontramos?**\nLos resultados indican que su hijo(a) presenta algunas características que pueden estar relacionadas con el Trastorno del Espectro Autista (TEA). Esto significa que su forma de comunicarse, relacionarse y procesar la información es diferente, no incorrecta.\n\n**¿Qué recomendamos?**\n1. **En casa:** Establecer rutinas claras, usar imágenes para anticipar actividades, y celebrar sus intereses especiales como fortalezas.\n2. **En la escuela:** Trabajaremos con el equipo de USAER para brindar los apoyos necesarios.\n3. **Salud:** Sugerimos una valoración con neurólogo pediatra para tener un diagnóstico completo.\n\n**¿Qué NO significa?**\nNo significa que no pueda aprender, ni que no pueda tener amigos. Solo significa que necesita apoyos específicos para desarrollar todo su potencial.\n\nEstamos aquí para apoyarles. No están solos en este proceso.\n\n--- FIN REPORTE FAMILIA ---",
  "confianza_analisis": 0.92,
  "modelo_utilizado": "llama3-tea-clinical:70b-q4_k_m",
  "tiempo_procesamiento_ms": 4850
}
```

---

## 9. PROMPT ENGINEERING CLÍNICO

### 9.1 Estructura del Prompt Base

```yaml
# config/prompts/base_tea.yaml
system_prompt: |
  Eres un psicólogo clínico especialista en Trastorno del Espectro Autista (TEA) 
  con 20 años de experiencia en diagnóstico y evaluación infantil. Tu expertise cubre:
  
  - Criterios DSM-5 para TEA (dominios A, B, C, D, E)
  - Evaluación dimensional de severidad (niveles 1, 2, 3)
  - Diagnóstico diferencial (TEA vs. TEL vs. TDAH vs. ansiedad social)
  - Recomendaciones basadas en evidencia (intervenciones educativas, terapéuticas, familiares)
  - Contexto educativo mexicano (USAER, CAM, inclusión educativa)
  
  Reglas estrictas:
  1. NUNCA diagnostiques definitivamente (solo "perfil compatible con" o "sugiere")
  2. Siempre recomienda evaluación por especialista para confirmación
  3. Se sensible al contexto cultural y familiar
  4. En banderas rojas, especifica plazo de acción
  5. Los reportes para docentes deben ser prácticos y accionables
  6. Los reportes para familias deben ser empáticos y libres de jerga técnica

user_prompt_template: |
  ## DATOS DEL TAMIZAJE TEA
  
  **Contexto observacional:**
  {contexto_obs}
  
  **Antecedentes relevantes:**
  {antecedentes}
  
  **Respuestas por indicador:**
  {answers_formatted}
  
  ## MÉTRICAS PRECALCULADAS
  
  | Dominio | Puntaje | Interpretación Base |
  |---------|---------|-------------------|
  | Interacción Social | {social_score}/12 | {social_level} |
  | Comunicación | {comunicacion_score}/12 | {comunicacion_level} |
  | Comportamientos Repetitivos | {repetitivo_score}/12 | {repetitivo_level} |
  | Sensorial | {sensorial_score}/12 | {sensorial_level} |
  | **Total** | **{total_score}/48** | **{total_level}** |
  
  ## INSTRUCCIONES ESPECÍFICAS
  
  Basado en los datos del tamizaje TEA, genera un análisis clínico completo 
  siguiendo EXACTAMENTE esta estructura JSON (sin markdown, sin texto adicional):
  
  {output_schema_json}
  
  IMPORTANTE: 
  - Los puntajes deben ser coherentes con las métricas precalculadas
  - Las recomendaciones deben ser específicas y accionables en contexto escolar mexicano
  - Los reportes narrativos deben estar en español neutral

output_schema:
  type: object
  required:
    - perfil_severidad
    - dominios_afectados
    - patrones_detectados
    - banderas_rojas
    - nivel_intervencion
    - recomendaciones
    - requiere_canalizacion
    - especialista_sugerido
    - resumen_clinico
    - reporte_docente
    - reporte_familia
    - confianza_analisis
```

### 9.2 Validación Post-LLM (Guardrails Clínicos)

```python
# core/response_parser.py

class ClinicalGuardrails:
    """Valida y sanitiza la respuesta del LLM antes de devolverla al Rust"""
    
    @staticmethod
    def validate_severity_consistency(response: dict, domain_scores: dict) -> bool:
        """Verifica que el nivel de severidad sea consistente con los puntajes"""
        score_map = {
            "Leve": (0, 16),
            "Moderado": (17, 32),
            "Severo": (33, 48)
        }
        nivel = response["perfil_severidad"]["nivel_global"]
        total = domain_scores["total"]
        min_s, max_s = score_map.get(nivel, (0, 48))
        return min_s <= total <= max_s
    
    @staticmethod
    def sanitize_recommendations(recommendations: list) -> list:
        """Filtra recomendaciones que podrían ser dañinas o inapropiadas"""
        forbidden_patterns = [
            "medicación sin prescripción",
            "terapias no avaladas",
            "restricción de derechos",
        ]
        return [
            rec for rec in recommendations
            if not any(p in rec["descripcion"].lower() for p in forbidden_patterns)
        ]
    
    @staticmethod
    def validate_report_length(report: str, max_chars: int = 3000) -> str:
        """Trunca reportes demasiado largos"""
        if len(report) > max_chars:
            return report[:max_chars] + "\n\n[...]"
        return report
```

---

## 10. SEGURIDAD Y PRIVACIDAD

### 10.1 Datos Sensibles (Consideraciones RGPD / LFPDPPP)

Los datos clínicos de menores de edad son **altamente sensibles**. Se requiere:

1. **Cifrado en tránsito:** TLS 1.3 entre todos los servicios
2. **Cifrado en reposo:** Datos cifrados en PostgreSQL (pgcrypto + column-level encryption)
3. **Autenticación servicio-a-servicio:** API Key rotada cada 90 días + HMAC signing
4. **Logs sin PII:** Nunca registrar nombres, direcciones, o identificadores directos
5. **Retención de datos:** Política de 5 años, luego anonimización
6. **Consentimiento:** Cada análisis debe registrar consentimiento del tutor

### 10.2 Esquema de Autenticación entre Servicios

```
┌──────────────────┐           ┌──────────────────┐
│  Rust Service    │           │  Agent Python    │
│  (back-end-      │           │  (agente-tea)    │
│   clinical)      │           │                  │
└────────┬─────────┘           └────────┬─────────┘
         │                              │
         │  1. Generar nonce (UUID v4)  │
         │  2. Crear payload:           │
         │     timestamp + nonce + path │
         │  3. Firmar con HMAC-SHA256   │
         │     usando API_KEY secreta   │
         │                              │
         │  POST /api/v1/analyze/tea    │
         │  X-API-Key: <api_key>        │
         │  X-Timestamp: 1712345678     │
         │  X-Nonce: a1b2c3d4...        │
         │  X-Signature: hmac_sha256    │
         │─────────────────────────────►│
         │                              │
         │              4. Validar:     │
         │  ◄───────────────────────────│ - timestamp (±5 min)
         │                              │ - nonce no usado antes
         │                              │ - HMAC coincide
         │                              │
```

### 10.3 Rate Limiting

```python
# Implementación en FastAPI

from fastapi import HTTPException, Request
from datetime import datetime, timedelta
import redis

class RateLimiter:
    def __init__(self, redis_client):
        self.redis = redis_client
    
    async def check_rate_limit(
        self, 
        request: Request,
        service_name: str,
        max_requests: int = 100,
        window_minutes: int = 1
    ):
        key = f"ratelimit:{service_name}:{request.client.host}"
        current = self.redis.incr(key)
        if current == 1:
            self.redis.expire(key, window_minutes * 60)
        if current > max_requests:
            raise HTTPException(
                status_code=429,
                detail="Too many requests. Please try again later."
            )
```

---

## 11. PLAN DE IMPLEMENTACIÓN

### Fase 1: Infraestructura Cloud (Semanas 1-2)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 1.1 Provisionar instancia GPU en nube | 1 día | — |
| 1.2 Configurar NVIDIA drivers + Docker | 1 día | 1.1 |
| 1.3 Desplegar Ollama con modelo base | 2 días | 1.2 |
| 1.4 Configurar Nginx + SSL + Load Balancer | 1 día | 1.3 |
| 1.5 Configurar Redis + monitoreo | 1 día | 1.4 |
| 1.6 Benchmark de latencia del modelo base | 2 días | 1.5 |

### Fase 2: Agente Python (Semanas 3-4)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 2.1 Crear proyecto FastAPI con estructura | 1 día | — |
| 2.2 Implementar cliente Ollama + prompt builder | 2 días | — |
| 2.3 Implementar response parser + guardrails | 2 días | — |
| 2.4 Implementar endpoints del agente | 2 días | 2.1-2.3 |
| 2.5 Implementar caché con Redis | 1 día | 2.4 |
| 2.6 Pruebas unitarias del agente | 2 días | 2.5 |

### Fase 3: Integración Rust (Semanas 5)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 3.1 Agregar `reqwest` + crear `agent_client.rs` | 1 día | — |
| 3.2 Crear DTOs de comunicación (`agent.rs`) | 1 día | — |
| 3.3 Modificar `tea_handler.rs` para llamar al agente | 1 día | 3.1-3.2 |
| 3.4 Agregar rutas del agente a `main.rs` | 0.5 días | 3.3 |
| 3.5 Configurar manejo de errores y timeouts | 1 día | 3.4 |
| 3.6 Pruebas de integración Rust-Agente | 2 días | 3.5 |

### Fase 4: Fine-Tuning del Modelo (Semanas 6-8)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 4.1 Recopilar dataset clínico (casos sintéticos) | 1 semana | — |
| 4.2 Anotar datos con expertos clínicos | 1 semana | 4.1 |
| 4.3 Realizar fine-tuning con LoRA/QLoRA | 3 días | 4.2 |
| 4.4 Evaluar modelo vs. baseline | 2 días | 4.3 |
| 4.5 Desplegar modelo fine-tuned en Ollama | 1 día | 4.4 |

### Fase 5: Validación Clínica (Semanas 9-10)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 5.1 Validar con 50 casos reales (retrospectivos) | 1 semana | Fase 4 |
| 5.2 Comparar con evaluación de psicólogos humanos | 1 semana | 5.1 |
| 5.3 Ajustar prompts basado en feedback | 3 días | 5.2 |
| 5.4 Documentar precisión y limitaciones | 2 días | 5.3 |

### Fase 6: Producción (Semana 11)

| Tarea | Duración | Dependencias |
|-------|----------|--------------|
| 6.1 Deploy a producción | 1 día | Fase 5 |
| 6.2 Monitoreo de latencia y calidad | 1 semana | 6.1 |
| 6.3 Documentación final | 2 días | 6.2 |
| 6.4 Capacitación a usuarios | 2 días | 6.3 |

---

## 12. MÉTRICAS Y EVALUACIÓN

### 12.1 Métricas Técnicas

| Métrica | Objetivo | Alerta | Crítico |
|---------|----------|--------|---------|
| Latencia P50 análisis | < 5s | > 8s | > 15s |
| Latencia P95 análisis | < 10s | > 15s | > 30s |
| Tasa de éxito (HTTP 200) | > 99% | < 98% | < 95% |
| Cache hit ratio | > 30% | < 20% | < 10% |
| Uso de VRAM | < 20GB | > 22GB | > 24GB |
| Uptime del servicio | > 99.9% | < 99.5% | < 99% |

### 12.2 Métricas Clínicas

| Métrica | Método de Medición | Objetivo |
|---------|-------------------|----------|
| Precisión diagnóstica | Comparación con equipo clínico | > 85% |
| Sensibilidad | Detección de casos TEA confirmados | > 90% |
| Especificidad | No falsear casos negativos | > 80% |
| Consistencia test-retest | Mismo caso analizado 3 veces | > 95% |
| Utilidad reportes | Encuesta a psicólogos usuarios | > 4.0/5.0 |

### 12.3 Dashboard de Monitoreo (Grafana)

```json
{
  "panels": [
    {
      "title": "Latencia del Agente (P50/P95/P99)",
      "type": "timeseries",
      "targets": ["agent_latency_seconds"]
    },
    {
      "title": "Tasa de Éxito vs Errores",
      "type": "stat",
      "targets": ["agent_requests_total", "agent_errors_total"]
    },
    {
      "title": "Distribución de Niveles de Severidad",
      "type": "piechart",
      "targets": ["tea_severity_distribution"]
    },
    {
      "title": "Uso de VRAM en Ollama",
      "type": "gauge",
      "targets": ["ollama_vram_usage_bytes"]
    },
    {
      "title": "Análisis por Hora",
      "type": "barchart",
      "targets": ["agent_requests_hourly"]
    }
  ]
}
```

---

## 13. PRESUPUESTO DE RECURSOS NUBE

### 13.1 Costos Mensuales Estimados

| Recurso | Especificación | Costo/Mes |
|---------|---------------|-----------|
| **GPU Instance** (AWS g5.xlarge) | 1x A10G, 24GB VRAM, 4 vCPU, 16GB RAM | ~$720 |
| **Load Balancer** (ALB) | Tráfico estimado 50GB/mes | ~$25 |
| **Redis** (ElastiCache) | cache.t3.small | ~$20 |
| **Storage** (EBS gp3) | 100GB SSD | ~$10 |
| **Data Transfer** | Salida estimada 100GB/mes | ~$10 |
| **Backup/Snapshots** | 50GB | ~$5 |
| **Monitoreo** (CloudWatch) | Logs + métricas | ~$15 |
| **Dominio + SSL** | 1 año (prorrateado) | ~$5 |
| **Total Estimado** | | **~$810/mes** |

### 13.2 Optimización de Costos

| Estrategia | Ahorro Estimado | Impacto |
|-----------|----------------|---------|
| **Spot instance** (AWS Spot) | -60-70% | Riesgo de interrupción |
| **Reserved instance** (1 año) | -40% | Sin riesgo |
| **GPU compartida** (RunPod/Vast.ai) | -50-60% | Menor control |
| **Modelo cuantizado Q4_K_M** | -40% VRAM | Mínima pérdida calidad |
| **Caching en Redis** | -30% llamadas LLM | Misma calidad |

### 13.3 Recomendación Final de Infraestructura

```
┌─────────────────────────────────────────────────────────────┐
│              RECOMENDACIÓN: RUNPOD SERVERLESS                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  • Costo: ~$0.50/hr → ~$360/mes (-55% vs AWS on-demand)   │
│  • GPU: NVIDIA A100 40GB (modelos más grandes)              │
│  • Serverless: paga solo por uso                            │
│  • API compatible con OpenAI → migración futura fácil       │
│  • Latencia: ~2-4s (comparable a AWS)                      │
│  • Incluye: storage, networking, monitoreo básico           │
│                                                             │
│  Alternativa: AWS g5.xlarge Reserved → ~$432/mes            │
│  (mejor integración con ecosistema USEBEQ si ya usan AWS)   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## ANEXO A: DEPENDENCIAS RUST A AGREGAR

```toml
# En Cargo.toml (agregar a [dependencies])
reqwest = { version = "0.12", features = ["json"] }
tokio = { version = "1.0", features = ["full", "time"] }
```

## ANEXO B: DEPENDENCIAS PYTHON

```txt
# requirements.txt
fastapi==0.111.0
uvicorn[standard]==0.29.0
httpx==0.27.0
pydantic==2.7.0
pydantic-settings==2.2.0
redis==5.0.0
python-dotenv==1.0.1
prometheus-client==0.20.0
structlog==24.1.0
tenacity==8.2.3
pyyaml==6.0.1
```

## ANEXO C: ARQUITECTURA DE DESPLIEGUE (DIAGRAMA K8S)

```yaml
# agent-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agente-tea
spec:
  replicas: 2
  selector:
    matchLabels:
      app: agente-tea
  template:
    metadata:
      labels:
        app: agente-tea
    spec:
      containers:
      - name: agent-api
        image: usebeq/agente-tea:latest
        ports:
        - containerPort: 8000
        env:
        - name: OLLAMA_HOST
          value: "http://ollama-service:11434"
        resources:
          requests:
            memory: "4Gi"
            cpu: "2"
          limits:
            memory: "8Gi"
            cpu: "4"
---
apiVersion: v1
kind: Service
metadata:
  name: agente-tea-service
spec:
  selector:
    app: agente-tea
  ports:
  - port: 443
    targetPort: 8000
  type: LoadBalancer
```

---


---

> Documento generado para USEBEQ — Proyecto: Back-end-Clinical + Agente Especialista TEA
> Versión: 1.0 | Fecha: Mayo 2026
