using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260626000000_AddSesionesPsicologicas")]
public partial class AddSesionesPsicologicas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE sesiones_psicologicas (
                id            SERIAL PRIMARY KEY,
                alumno_id     UUID        NOT NULL,
                psicologo_id  UUID        NOT NULL,
                ciclo_id      INT         NOT NULL,
                fecha         DATE        NOT NULL DEFAULT CURRENT_DATE,
                tipo          VARCHAR(40),
                motivo        TEXT,
                nota          TEXT        NOT NULL,
                acuerdos      TEXT,
                created_at    TIMESTAMPTZ DEFAULT NOW()
            );
            CREATE INDEX idx_sesiones_alumno ON sesiones_psicologicas(alumno_id, ciclo_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS sesiones_psicologicas;");
    }
}
