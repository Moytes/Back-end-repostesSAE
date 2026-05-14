use serde::{Deserialize, Serialize};
use uuid::Uuid;
use chrono::{DateTime, Utc, NaiveDate};

#[allow(dead_code)]
#[derive(Debug, Serialize, Deserialize, sqlx::FromRow)]
pub struct TeaScreening {
    pub id: i32,
    pub alumno_id: Uuid,
    pub evaluador_id: Uuid,
    pub ciclo_id: i32,
    pub fecha: NaiveDate,
    pub contexto_obs: Option<String>,
    pub observaciones_generales: Option<String>,
    pub puntaje_total: Option<i16>,
    pub nivel_alerta: Option<String>,
    pub requiere_canalizacion: bool,
    pub created_at: DateTime<Utc>,
}

#[allow(dead_code)]
#[derive(Debug, Serialize, Deserialize, sqlx::FromRow)]
pub struct TeaRespuesta {
    pub id: i32,
    pub screening_id: i32,
    pub indicador_id: i32,
    pub frecuencia: i16,
    pub intensidad: i16,
    pub observacion: Option<String>,
}

#[allow(dead_code)]
#[derive(Debug, Serialize, Deserialize, sqlx::FromRow)]
pub struct CieEvaluacion {
    pub id: i32,
    pub alumno_id: Uuid,
    pub evaluador_id: Uuid,
    pub ciclo_id: i32,
    pub dimension_id: i32,
    pub fecha: NaiveDate,
    pub observaciones: Option<String>,
    pub estado: String,
    pub created_at: DateTime<Utc>,
}

#[allow(dead_code)]
#[derive(Debug, Serialize, Deserialize, sqlx::FromRow)]
pub struct CieRespuesta {
    pub id: i32,
    pub evaluacion_id: i32,
    pub subindicador_id: i32,
    pub logrado: Option<bool>,
    pub nivel_ayuda: Option<i16>,
    pub respuesta_tipo: Option<String>,
    pub observacion: Option<String>,
}
