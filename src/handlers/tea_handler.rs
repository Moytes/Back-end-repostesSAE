use axum::{Json, extract::State};
use axum::response::IntoResponse;
use crate::middleware::auth::AuthenticatedUser;
use crate::models::response::ApiResponse;
use crate::services::tea_scoring::TeaScoringService;
use crate::AppState;
use axum::http::StatusCode;
use serde::Deserialize;
use std::sync::Arc;
use uuid::Uuid;

#[derive(Deserialize)]
pub struct TeaEvaluationRequest {
    pub student_id: Uuid,
    pub ciclo_id: i32,
    pub answers: Vec<i32>,
    pub contexto_obs: Option<String>,
}

pub async fn evaluate_tea(
    State(state): State<Arc<AppState>>,
    AuthenticatedUser(claims): AuthenticatedUser,
    Json(payload): Json<TeaEvaluationRequest>,
) -> impl IntoResponse {
    tracing::info!("Evaluating TEA for student {} by user {}", payload.student_id, claims.sub);

    // 1. Ejecutar el motor de cálculo en Rust
    let result = TeaScoringService::calculate_score(&payload.answers);

    // 2. Persistir en la base de datos
    let user_id = match Uuid::parse_str(&claims.sub) {
        Ok(id) => id,
        Err(_) => return (StatusCode::BAD_REQUEST, Json(ApiResponse::new(400, "CL_", "Invalid user ID in token".to_string()))).into_response(),
    };

    let save_result = sqlx::query(
        r#"
        INSERT INTO tea_screenings 
        (alumno_id, evaluador_id, ciclo_id, puntaje_total, nivel_alerta, contexto_obs)
        VALUES ($1, $2, $3, $4, $5, $6)
        RETURNING id
        "#
    )
    .bind(payload.student_id)
    .bind(user_id)
    .bind(payload.ciclo_id)
    .bind(result.total_score as i16)
    .bind(&result.alert_level)
    .bind(&payload.contexto_obs)
    .fetch_one(&state.db)
    .await;

    match save_result {
        Ok(_record) => {
            // Con sqlx::query (no macro), el record es un PgRow. 
            // Podríamos extraer el id si fuera necesario: let id: i32 = record.get("id");
            tracing::info!("TEA evaluation saved successfully");
            let response = ApiResponse::new(
                200,
                "CL_",
                result
            );
            (StatusCode::OK, Json(response)).into_response()
        }
        Err(e) => {
            tracing::error!("Failed to save TEA evaluation: {}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, Json(ApiResponse::new(500, "CL_", "Error saving to database".to_string()))).into_response()
        }
    }
}

#[derive(Deserialize)]
pub struct TeaHistoryParams {
    pub student_id: Uuid,
}

pub async fn get_tea_history(
    State(state): State<Arc<AppState>>,
    AuthenticatedUser(_claims): AuthenticatedUser,
    axum::extract::Query(params): axum::extract::Query<TeaHistoryParams>,
) -> impl IntoResponse {
    tracing::info!("Fetching TEA history for student {}", params.student_id);

    let screenings = sqlx::query_as::<_, crate::models::clinical::TeaScreening>(
        "SELECT * FROM tea_screenings WHERE alumno_id = $1 ORDER BY fecha DESC"
    )
    .bind(params.student_id)
    .fetch_all(&state.db)
    .await;

    match screenings {
        Ok(list) => {
            let response = ApiResponse::new(200, "CL_", list);
            (StatusCode::OK, Json(response)).into_response()
        }
        Err(e) => {
            tracing::error!("Failed to fetch TEA history: {}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, Json(ApiResponse::new(500, "CL_", "Error fetching history".to_string()))).into_response()
        }
    }
}
