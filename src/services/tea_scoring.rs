use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct TeaScoringResult {
    pub total_score: u32,
    pub alert_level: String,
}

pub struct TeaScoringService;

impl TeaScoringService {
    /// Calcula la puntuación total y el nivel de alerta para un tamizaje TEA.
    /// Rust brilla aquí por su seguridad en el manejo de datos numéricos y velocidad.
    pub fn calculate_score(answers: &[i32]) -> TeaScoringResult {
        let total: i32 = answers.iter().sum();
        
        let alert_level = match total {
            0..=2 => "Bajo",
            3..=7 => "Medio",
            _ => "Alto",
        };

        TeaScoringResult {
            total_score: total as u32,
            alert_level: alert_level.to_string(),
        }
    }
}
