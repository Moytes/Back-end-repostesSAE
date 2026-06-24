using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260623000000_InitialReportsSchema")]
public partial class InitialReportsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

            CREATE TABLE canalizaciones (
                id              SERIAL PRIMARY KEY,
                alumno_id       UUID        NOT NULL,
                ciclo_id        INT         NOT NULL,
                fecha           DATE        NOT NULL DEFAULT CURRENT_DATE,
                area_canaliza   INT,
                motivo          TEXT        NOT NULL,
                acciones_aula   TEXT,
                solicitante_id  UUID,
                receptor_id     UUID,
                fecha_recibido  DATE,
                estado          VARCHAR(20) DEFAULT 'PENDIENTE'
                                CHECK (estado IN ('PENDIENTE','RECIBIDA','EN_PROCESO','CERRADA')),
                created_at      TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_canalizaciones_estado ON canalizaciones(estado, ciclo_id);
            CREATE INDEX idx_canalizaciones_alumno ON canalizaciones(alumno_id, ciclo_id);

            CREATE TABLE evaluaciones_psicopedagogicas (
                id                          SERIAL PRIMARY KEY,
                alumno_id                   UUID NOT NULL,
                ciclo_id                    INT  NOT NULL,
                fecha_elaboracion           DATE NOT NULL DEFAULT CURRENT_DATE,
                motivo_evaluacion           TEXT,
                conducta_evaluacion         TEXT,
                antecedentes_embarazo       TEXT,
                antecedentes_heredo         TEXT,
                desarrollo_motor            TEXT,
                desarrollo_lenguaje         TEXT,
                historia_medica             TEXT,
                historia_escolar            TEXT,
                situacion_familiar          TEXT,
                descripcion_alumno          TEXT,
                contexto_familiar           TEXT,
                contexto_escolar            TEXT,
                contexto_social             TEXT,
                desarrollo_fisico           TEXT,
                desarrollo_cognitivo        TEXT,
                desarrollo_socioafectivo    TEXT,
                evaluacion_aprendizajes     TEXT,
                creatividad                 TEXT,
                interpretacion_resultados   TEXT,
                conclusiones                TEXT,
                estado      VARCHAR(20) DEFAULT 'BORRADOR'
                            CHECK (estado IN ('BORRADOR','EN_REVISION','FIRMADA','ENTREGADA')),
                created_at  TIMESTAMPTZ DEFAULT NOW(),
                updated_at  TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_eval_psico_alumno ON evaluaciones_psicopedagogicas(alumno_id, ciclo_id);

            CREATE TABLE eval_psico_bap (
                id                SERIAL PRIMARY KEY,
                eval_psico_id     INT NOT NULL REFERENCES evaluaciones_psicopedagogicas(id) ON DELETE CASCADE,
                tipo_bap          VARCHAR(100),
                contexto          VARCHAR(100),
                indicador_inclusion TEXT,
                descripcion       TEXT
            );

            CREATE TABLE eval_psico_colaboradores (
                id              SERIAL PRIMARY KEY,
                eval_psico_id   INT  NOT NULL REFERENCES evaluaciones_psicopedagogicas(id) ON DELETE CASCADE,
                usuario_id      UUID,
                nombre_externo  VARCHAR(200),
                rol_colaborador VARCHAR(100),
                firma_digital   BOOLEAN DEFAULT FALSE,
                fecha_firma     TIMESTAMPTZ
            );

            CREATE TABLE cie_evaluaciones (
                id              SERIAL PRIMARY KEY,
                alumno_id       UUID        NOT NULL,
                evaluador_id    UUID        NOT NULL,
                ciclo_id        INT         NOT NULL,
                dimension_id    INT         NOT NULL,
                fecha           DATE        NOT NULL DEFAULT CURRENT_DATE,
                observaciones   TEXT,
                estado          VARCHAR(20) DEFAULT 'EN_PROCESO'
                                CHECK (estado IN ('EN_PROCESO','COMPLETADA','REVISADA')),
                created_at      TIMESTAMPTZ DEFAULT NOW(),
                updated_at      TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_cie_eval_alumno ON cie_evaluaciones(alumno_id, ciclo_id);

            CREATE TABLE cie_respuestas (
                id              SERIAL PRIMARY KEY,
                evaluacion_id   INT NOT NULL REFERENCES cie_evaluaciones(id) ON DELETE CASCADE,
                subindicador_id INT NOT NULL,
                logrado         BOOLEAN,
                nivel_ayuda     SMALLINT CHECK (nivel_ayuda BETWEEN 0 AND 4),
                respuesta_tipo  VARCHAR(20) CHECK (respuesta_tipo IN ('COMUNICATIVO','LINGUISTICO')),
                observacion     TEXT,
                evidencia_url   TEXT,
                UNIQUE(evaluacion_id, subindicador_id)
            );

            CREATE TABLE cie_resp_fonoarticulador (
                id              SERIAL PRIMARY KEY,
                evaluacion_id   INT NOT NULL REFERENCES cie_evaluaciones(id) ON DELETE CASCADE,
                subindicador_id INT NOT NULL,
                funcional       BOOLEAN,
                observacion_forma TEXT,
                UNIQUE(evaluacion_id, subindicador_id)
            );

            CREATE TABLE tea_screenings (
                id                      SERIAL PRIMARY KEY,
                alumno_id               UUID        NOT NULL,
                evaluador_id            UUID        NOT NULL,
                ciclo_id                INT         NOT NULL,
                fecha                   DATE        NOT NULL DEFAULT CURRENT_DATE,
                contexto_obs            VARCHAR(50),
                observaciones_generales TEXT,
                puntaje_total           SMALLINT,
                nivel_alerta            VARCHAR(20)
                                        CHECK (nivel_alerta IN ('SIN_ALERTA','LEVE','MODERADO','SIGNIFICATIVO')),
                requiere_canalizacion   BOOLEAN DEFAULT FALSE,
                created_at              TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_tea_screen_alumno ON tea_screenings(alumno_id, ciclo_id);

            CREATE TABLE tea_respuestas (
                id              SERIAL PRIMARY KEY,
                screening_id    INT NOT NULL REFERENCES tea_screenings(id) ON DELETE CASCADE,
                indicador_id    INT NOT NULL,
                frecuencia      SMALLINT CHECK (frecuencia BETWEEN 0 AND 3),
                intensidad      SMALLINT CHECK (intensidad BETWEEN 0 AND 3),
                observacion     TEXT,
                UNIQUE(screening_id, indicador_id)
            );

            CREATE OR REPLACE FUNCTION fn_calcular_puntaje_tea()
            RETURNS TRIGGER AS $$
            BEGIN
                UPDATE tea_screenings SET
                    puntaje_total = sub.total,
                    nivel_alerta = CASE
                        WHEN sub.total >= 30 THEN 'SIGNIFICATIVO'
                        WHEN sub.total >= 20 THEN 'MODERADO'
                        WHEN sub.total >= 10 THEN 'LEVE'
                        ELSE 'SIN_ALERTA'
                    END
                FROM (
                    SELECT COALESCE(SUM(frecuencia + intensidad), 0) AS total
                    FROM tea_respuestas WHERE screening_id = NEW.screening_id
                ) sub
                WHERE id = NEW.screening_id;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_tea_puntaje
            AFTER INSERT OR UPDATE ON tea_respuestas
            FOR EACH ROW EXECUTE FUNCTION fn_calcular_puntaje_tea();

            CREATE TABLE reportes (
                id              SERIAL PRIMARY KEY,
                tipo            VARCHAR(50) NOT NULL,
                alumno_id       UUID,
                grupo_id        INT,
                ciclo_id        INT         NOT NULL,
                generado_por    UUID        NOT NULL,
                parametros_json JSONB,
                contenido_json  JSONB,
                archivo_r2_key  TEXT,
                archivo_url     TEXT,
                formato         VARCHAR(10) DEFAULT 'PDF',
                created_at      TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_reportes_alumno ON reportes(alumno_id, ciclo_id);
            CREATE INDEX idx_reportes_tipo   ON reportes(tipo, ciclo_id);

            CREATE TABLE audit_log (
                id              BIGSERIAL PRIMARY KEY,
                servicio_origen VARCHAR(30) NOT NULL
                                CHECK (servicio_origen IN ('USERS','MATERIALS','REPORTS')),
                usuario_id      UUID,
                accion          VARCHAR(50)  NOT NULL,
                tabla_afectada  VARCHAR(100),
                registro_id     TEXT,
                datos_previos   JSONB,
                datos_nuevos    JSONB,
                ip_address      INET,
                created_at      TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_audit_usuario ON audit_log(usuario_id, created_at DESC);
            CREATE INDEX idx_audit_tabla   ON audit_log(tabla_afectada, created_at DESC);
            CREATE INDEX idx_audit_servicio ON audit_log(servicio_origen, created_at DESC);

            CREATE VIEW v_resumen_cie AS
            SELECT
                ce.alumno_id,
                ce.dimension_id,
                ce.fecha,
                COUNT(cr.id)                                        AS total_items,
                COUNT(cr.id) FILTER (WHERE cr.logrado = TRUE)       AS logrados,
                ROUND(
                    100.0 * COUNT(cr.id) FILTER (WHERE cr.logrado = TRUE)
                    / NULLIF(COUNT(cr.id), 0), 1
                )                                                   AS pct_logro,
                ce.estado
            FROM cie_evaluaciones ce
            LEFT JOIN cie_respuestas cr ON cr.evaluacion_id = ce.id
            GROUP BY ce.alumno_id, ce.dimension_id, ce.fecha, ce.estado, ce.id;

            CREATE VIEW v_alertas_tea AS
            SELECT
                ts.alumno_id,
                ts.fecha,
                ts.puntaje_total,
                ts.nivel_alerta,
                ts.requiere_canalizacion,
                COUNT(tr.id) FILTER (WHERE tr.frecuencia >= 2) AS indicadores_frecuentes
            FROM tea_screenings ts
            LEFT JOIN tea_respuestas tr ON tr.screening_id = ts.id
            GROUP BY ts.alumno_id, ts.fecha, ts.puntaje_total,
                     ts.nivel_alerta, ts.requiere_canalizacion, ts.id;

            CREATE OR REPLACE FUNCTION fn_set_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_eval_psico_upd BEFORE UPDATE ON evaluaciones_psicopedagogicas
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
            CREATE TRIGGER trg_cie_eval_upd   BEFORE UPDATE ON cie_evaluaciones
                FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP VIEW IF EXISTS v_alertas_tea;
            DROP VIEW IF EXISTS v_resumen_cie;

            DROP TRIGGER IF EXISTS trg_cie_eval_upd ON cie_evaluaciones;
            DROP TRIGGER IF EXISTS trg_eval_psico_upd ON evaluaciones_psicopedagogicas;
            DROP FUNCTION IF EXISTS fn_set_updated_at();

            DROP TRIGGER IF EXISTS trg_tea_puntaje ON tea_respuestas;
            DROP FUNCTION IF EXISTS fn_calcular_puntaje_tea();

            DROP TABLE IF EXISTS audit_log;
            DROP TABLE IF EXISTS reportes;
            DROP TABLE IF EXISTS tea_respuestas;
            DROP TABLE IF EXISTS tea_screenings;
            DROP TABLE IF EXISTS cie_resp_fonoarticulador;
            DROP TABLE IF EXISTS cie_respuestas;
            DROP TABLE IF EXISTS cie_evaluaciones;
            DROP TABLE IF EXISTS eval_psico_colaboradores;
            DROP TABLE IF EXISTS eval_psico_bap;
            DROP TABLE IF EXISTS evaluaciones_psicopedagogicas;
            DROP TABLE IF EXISTS canalizaciones;
            """);
    }
}
