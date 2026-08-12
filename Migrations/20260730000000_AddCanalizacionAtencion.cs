using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations;

[DbContext(typeof(ReportsDbContext))]
[Migration("20260730000000_AddCanalizacionAtencion")]
public partial class AddCanalizacionAtencion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE canalizaciones
                ADD COLUMN tipo_atencion VARCHAR(80),
                ADD COLUMN fecha_atencion DATE,
                ADD COLUMN observaciones_clinicas TEXT,
                ADD COLUMN derivar_area_id INT,
                ADD COLUMN prioridad VARCHAR(10) DEFAULT 'MEDIA'
                    CHECK (prioridad IN ('BAJA', 'MEDIA', 'ALTA'));

            CREATE INDEX idx_canalizaciones_derivar_area
                ON canalizaciones(derivar_area_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS idx_canalizaciones_derivar_area;
            ALTER TABLE canalizaciones
                DROP COLUMN IF EXISTS prioridad,
                DROP COLUMN IF EXISTS derivar_area_id,
                DROP COLUMN IF EXISTS observaciones_clinicas,
                DROP COLUMN IF EXISTS fecha_atencion,
                DROP COLUMN IF EXISTS tipo_atencion;
            """);
    }
}
