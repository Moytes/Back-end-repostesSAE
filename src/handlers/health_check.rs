use axum::{Json, response::IntoResponse};
use crate::models::response::ApiResponse;
use hyper::StatusCode;

pub async fn health_check() -> impl IntoResponse {
    let response = ApiResponse::new(
        200,
        "CL_",
        "Clinical Intelligence MS is healthy"
    );
    
    (StatusCode::OK, Json(response))
}
