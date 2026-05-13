use axum::{
    async_trait,
    extract::FromRequestParts,
    http::request::Parts,
};
use jsonwebtoken::{decode, DecodingKey, Validation};
use crate::models::auth::Claims;
use crate::models::response::ApiResponse;
use axum::Json;
use hyper::StatusCode;

pub struct AuthenticatedUser(pub Claims);

#[async_trait]
impl<S> FromRequestParts<S> for AuthenticatedUser
where
    S: Send + Sync,
{
    type Rejection = (StatusCode, Json<ApiResponse<String>>);

    async fn from_request_parts(parts: &Parts, _state: &S) -> Result<Self, Self::Rejection> {
        // 1. Extraer el header de Authorization
        let auth_header = parts
            .headers
            .get(axum::http::header::AUTHORIZATION)
            .and_then(|value| value.to_str().ok())
            .and_then(|value| value.strip_prefix("Bearer "));

        let token = auth_header.ok_or_else(|| {
            (
                StatusCode::UNAUTHORIZED,
                Json(ApiResponse::new(401, "CL_", "Missing authorization token".to_string())),
            )
        })?;

        // 2. Validar el JWT (usando la clave secreta compartida)
        let secret = std::env::var("JWT_SECRET").expect("JWT_SECRET must be set");
        
        let token_data = decode::<Claims>(
            token,
            &DecodingKey::from_secret(secret.as_ref()),
            &Validation::default(),
        )
        .map_err(|_| {
            (
                StatusCode::UNAUTHORIZED,
                Json(ApiResponse::new(401, "CL_", "Invalid or expired token".to_string())),
            )
        })?;

        Ok(AuthenticatedUser(token_data.claims))
    }
}
