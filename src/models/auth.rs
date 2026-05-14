use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct Claims {
    pub sub: String,      // UserId
    pub role: String,     // UserRole
    pub student_id: Option<String>,
    pub exp: usize,
}

#[allow(dead_code)]
#[derive(Debug, Serialize, Deserialize)]
pub struct AuthPayload {
    pub user_id: uuid::Uuid,
    pub role: String,
}
