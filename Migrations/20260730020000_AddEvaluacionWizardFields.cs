using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260730020000_AddEvaluacionWizardFields")]
public partial class AddEvaluacionWizardFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE evaluaciones_psicopedagogicas
                ADD COLUMN areas_evaluar TEXT[] NOT NULL DEFAULT ARRAY[]::text[],
                ADD COLUMN instrumentos_aplicar TEXT[] NOT NULL DEFAULT ARRAY[]::text[],
                ADD COLUMN instrumentos_detalle TEXT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE evaluaciones_psicopedagogicas
                DROP COLUMN IF EXISTS instrumentos_detalle,
                DROP COLUMN IF EXISTS instrumentos_aplicar,
                DROP COLUMN IF EXISTS areas_evaluar;
            """);
    }
}
