using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260730010000_AddCitasEspecialista")]
public partial class AddCitasEspecialista : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE citas_especialista (
                id                  SERIAL PRIMARY KEY,
                alumno_id           UUID NOT NULL,
                especialista_id     UUID NOT NULL,
                tea_screening_id    INT REFERENCES tea_screenings(id) ON DELETE SET NULL,
                tipo_cita           VARCHAR(60) NOT NULL,
                fecha               DATE NOT NULL,
                hora                TIME NOT NULL,
                modalidad           VARCHAR(15) NOT NULL
                                      CHECK (modalidad IN ('PRESENCIAL', 'VIRTUAL')),
                notas_tutor         TEXT,
                estado              VARCHAR(20) NOT NULL DEFAULT 'PROGRAMADA'
                                      CHECK (estado IN ('PROGRAMADA', 'REALIZADA', 'CANCELADA')),
                created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE INDEX idx_citas_especialista_alumno_fecha
                ON citas_especialista(alumno_id, fecha, hora);
            CREATE INDEX idx_citas_especialista_screening
                ON citas_especialista(tea_screening_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS citas_especialista;
            """);
    }
}
