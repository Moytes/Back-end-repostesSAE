using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260730030000_AddTeaAlertSeguimiento")]
public partial class AddTeaAlertSeguimiento : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE tea_screenings
                ADD COLUMN seguimiento_estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVA'
                    CHECK (seguimiento_estado IN ('ACTIVA', 'EN_MONITOREO', 'NOTIFICADA', 'RESUELTA')),
                ADD COLUMN seguimiento_at TIMESTAMPTZ,
                ADD COLUMN seguimiento_nota TEXT;

            CREATE INDEX idx_tea_screen_seguimiento
                ON tea_screenings(seguimiento_estado, seguimiento_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS idx_tea_screen_seguimiento;
            ALTER TABLE tea_screenings
                DROP COLUMN IF EXISTS seguimiento_nota,
                DROP COLUMN IF EXISTS seguimiento_at,
                DROP COLUMN IF EXISTS seguimiento_estado;
            """);
    }
}
