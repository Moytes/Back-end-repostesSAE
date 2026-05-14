use axum::{
    routing::{get, post},
    Router,
};
use std::net::SocketAddr;
use tower_http::cors::{Any, CorsLayer};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};
use sqlx::postgres::PgPoolOptions;
use std::sync::Arc;

mod handlers;
mod models;
mod middleware;
mod services;

pub struct AppState {
    pub db: sqlx::PgPool,
}

#[tokio::main]
async fn main() {
    // Cargar variables de entorno desde .env
    dotenvy::dotenv().ok();

    // 1. Inicializar logs profesionales
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "back_end_clinical=debug".into()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();

    // 2. Configurar Base de Datos
    let database_url = std::env::var("DATABASE_URL").expect("DATABASE_URL must be set");
    let pool = PgPoolOptions::new()
        .max_connections(5)
        .connect(&database_url)
        .await
        .expect("Failed to connect to the database");

    let state = Arc::new(AppState { db: pool });

    // 3. Configurar CORS (Alineado con el Gateway y Angular)
    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);

    // 4. Definir rutas con seguridad integrada
    let app = Router::new()
        // Ruta pública de salud
        .route("/api/clinical/health", get(handlers::health_check::health_check))
        
        // Rutas protegidas (El extractor AuthenticatedUser validará el JWT automáticamente)
        .route("/api/clinical/evaluate/tea", post(handlers::tea_handler::evaluate_tea))
        .route("/api/clinical/history/tea", get(handlers::tea_handler::get_tea_history))
        
        .layer(cors)
        .with_state(state);

    // 5. Iniciar servidor de alto rendimiento
    let port = std::env::var("PORT")
        .unwrap_or_else(|_| "5005".to_string())
        .parse::<u16>()
        .expect("PORT must be a valid number");

    let addr = SocketAddr::from(([0, 0, 0, 0], port));
    tracing::info!("Clinical Intelligence MS starting on {}", addr);
    
    let listener = tokio::net::TcpListener::bind(&addr).await.unwrap_or_else(|e| {
        tracing::error!("Failed to bind to port {}: {}", port, e);
        std::process::exit(1);
    });
    axum::serve(listener, app).await.unwrap();
}
