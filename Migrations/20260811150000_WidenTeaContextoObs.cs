using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_end_RepostesSAE.Migrations
{
    /// <inheritdoc />
    public partial class WidenTeaContextoObs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tea_screenings.contexto_obs se creó como VARCHAR(50), pero el formulario del
            // frontend lo expone como un textarea sin límite ("Contexto / observaciones
            // generales") — cualquier especialista que escribiera una oración normal tronaba
            // con un 500 ("value too long for type character varying(50)"). Se amplía a TEXT,
            // igual que observaciones_generales (la columna vecina, ya sin límite).
            migrationBuilder.Sql(
                "ALTER TABLE tea_screenings ALTER COLUMN contexto_obs TYPE TEXT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE tea_screenings ALTER COLUMN contexto_obs TYPE VARCHAR(50);");
        }
    }
}
