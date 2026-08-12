using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class ExpedienteRepository(IConfiguration configuration) : IExpedienteRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<ExpedienteAlumnoDto?> GetAlumnoBasico(Guid alumnoId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                s.id AS Id,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS NombreCompleto,
                s.curp AS Curp,
                enr.escuela_nombre AS EscuelaNombre,
                enr.grupo AS Grupo,
                enr.grado AS Grado,
                COALESCE((
                    SELECT array_agg(DISTINCT aa.name)
                    FROM student_attention_area saa
                    JOIN attention_area aa ON aa.id = saa.attention_area_id
                    WHERE saa.student_id = s.id
                ), ARRAY[]::text[]) AS AreasAtencion,
                COALESCE((
                    SELECT array_agg(DISTINCT d.name)
                    FROM student_disability sd
                    JOIN disability d ON d.id = sd.disability_id
                    WHERE sd.student_id = s.id
                ), ARRAY[]::text[]) AS Discapacidades
            FROM "student" s
            LEFT JOIN LATERAL (
                SELECT
                    gr.numero AS grado,
                    CONCAT(gr.numero, '° ', g.section) AS grupo,
                    sc.name AS escuela_nombre
                FROM "registration" r
                JOIN "group" g ON g.id = r.group_id
                JOIN "grade" gr ON gr.id = g.grade_id
                JOIN "school" sc ON sc.id = g.school_id
                WHERE r.student_id = s.id
                ORDER BY r.school_year_id DESC NULLS LAST
                LIMIT 1
            ) enr ON TRUE
            WHERE s.id = @AlumnoId;
            """;

        var row = await conn.QueryFirstOrDefaultAsync<ExpedienteAlumnoRow>(sql, new { AlumnoId = alumnoId });
        if (row == null)
            return null;

        return new ExpedienteAlumnoDto
        {
            Id = row.Id,
            NombreCompleto = row.NombreCompleto,
            Curp = row.Curp,
            EscuelaNombre = row.EscuelaNombre,
            Grupo = row.Grupo,
            Grado = row.Grado,
            AreasAtencion = row.AreasAtencion?.ToList() ?? [],
            Discapacidades = row.Discapacidades?.ToList() ?? []
        };
    }

    public async Task<IReadOnlyList<ExpedienteActividadDto>> GetActividades(Guid alumnoId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                aa.id                   AS Id,
                m.titulo                AS MaterialTitulo,
                aa.estado               AS Estado,
                a.fecha_asignacion      AS FechaAsignacion,
                a.fecha_limite          AS FechaLimite,
                aa.fecha_completado     AS FechaCompletado,
                aa.retroalimentacion    AS Retroalimentacion,
                a.instrucciones         AS Instrucciones
            FROM asignacion_alumnos aa
            JOIN asignaciones a ON a.id = aa.asignacion_id
            JOIN materiales m ON m.id = a.material_id
            WHERE aa.alumno_id = @AlumnoId
            ORDER BY a.fecha_asignacion DESC;
            """;

        var rows = await conn.QueryAsync<ExpedienteActividadDto>(sql, new { AlumnoId = alumnoId });
        return rows.ToList();
    }

    private sealed class ExpedienteAlumnoRow
    {
        public Guid Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Curp { get; set; }
        public string? EscuelaNombre { get; set; }
        public string? Grupo { get; set; }
        public short? Grado { get; set; }
        public string[]? AreasAtencion { get; set; }
        public string[]? Discapacidades { get; set; }
    }
}
