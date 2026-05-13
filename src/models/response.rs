use serde::Serialize;

#[derive(Serialize)]
pub struct ApiResponse<T> {
    pub status_code: u16,
    pub int_op_code: String,
    pub data: T,
}

impl<T> ApiResponse<T> {
    pub fn new(status_code: u16, prefix: &str, data: T) -> Self {
        Self {
            status_code,
            int_op_code: format!("{}{}", prefix, status_code),
            data,
        }
    }
}
