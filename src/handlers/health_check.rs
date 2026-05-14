use axum::{Json, response::IntoResponse, extract::State};
use crate::models::response::ApiResponse;
use crate::AppState;
use axum::http::StatusCode;
use std::sync::Arc;

pub async fn health_check(
    State(state): State<Arc<AppState>>,
) -> impl IntoResponse {
    // Verificar conexión a la base de datos
    let db_status = match sqlx::query("SELECT 1").execute(&state.db).await {
        Ok(_) => "Connected",
        Err(_) => "Disconnected",
    };

    let message = format!("Clinical Intelligence MS is healthy. DB: {}", db_status);
    let status_code = if db_status == "Connected" { StatusCode::OK } else { StatusCode::SERVICE_UNAVAILABLE };

    let response = ApiResponse::new(
        status_code.as_u16(),
        "CL_",
        message
    );
    
    (status_code, Json(response))
}
