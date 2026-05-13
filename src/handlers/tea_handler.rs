use axum::Json;
use axum::response::IntoResponse;
use crate::middleware::auth::AuthenticatedUser;
use crate::models::response::ApiResponse;
use crate::services::tea_scoring::TeaScoringService;
use hyper::StatusCode;
use serde::Deserialize;

#[derive(Deserialize)]
pub struct TeaEvaluationRequest {
    pub student_id: uuid::Uuid,
    pub answers: Vec<i32>,
}

pub async fn evaluate_tea(
    AuthenticatedUser(claims): AuthenticatedUser,
    Json(payload): Json<TeaEvaluationRequest>,
) -> impl IntoResponse {
    tracing::info!("Evaluating TEA for student {} by user {}", payload.student_id, claims.sub);

    // Ejecutar el motor de cálculo en Rust
    let result = TeaScoringService::calculate_score(&payload.answers);

    let response = ApiResponse::new(
        200,
        "CL_",
        result
    );

    (StatusCode::OK, Json(response))
}
