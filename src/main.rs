use axum::{
    routing::{get, post},
    Router,
};
use std::net::SocketAddr;
use tower_http::cors::{Any, CorsLayer};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

mod handlers;
mod models;
mod middleware;
mod services;

#[tokio::main]
async fn main() {
    // 1. Inicializar logs profesionales
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "back_end_clinical=debug".into()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();

    // 2. Configurar CORS (Alineado con el Gateway y Angular)
    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);

    // 3. Definir rutas con seguridad integrada
    let app = Router::new()
        // Ruta pública de salud
        .route("/api/clinical/health", get(handlers::health_check::health_check))
        
        // Rutas protegidas (El extractor AuthenticatedUser validará el JWT automáticamente)
        .route("/api/clinical/evaluate/tea", post(handlers::tea_handler::evaluate_tea))
        
        .layer(cors);

    // 4. Iniciar servidor de alto rendimiento
    let addr = SocketAddr::from(([0, 0, 0, 0], 5005));
    tracing::info!("Clinical Intelligence MS starting on {}", addr);
    
    let listener = tokio::net::TcpListener::bind(&addr).await.unwrap();
    axum::serve(listener, app).await.unwrap();
}
