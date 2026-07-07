using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class ClinicalReadRepository(IConfiguration configuration) : IClinicalReadRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    // CTE: alumnos dentro del alcance (escuela permitida + área de atención del psicólogo).
    private const string AlumnosScopeCte = """
        WITH alumnos_scope AS (
            SELECT DISTINCT s.id
            FROM "student" s
            LEFT JOIN "registration" r ON r.student_id = s.id
            LEFT JOIN "group" g ON g.id = r.group_id
            WHERE COALESCE(g.school_id, s.school_id) = ANY(@AllowedSchoolIds)
              AND EXISTS (
                  SELECT 1 FROM "student_attention_area" saa
                  WHERE saa.student_id = s.id AND saa.attention_area_id = @AreaId
              )
        )
        """;

    private const string StudentNameExpr =
        "TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, '')))";

    public async Task<IEnumerable<EvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int attentionAreaId, Guid? studentId, int? cicloId)
    {
        if (allowedSchoolIds.Length == 0) return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                e.id                       AS Id,
                e.alumno_id                AS StudentId,
                {StudentNameExpr}          AS StudentName,
                e.ciclo_id                 AS SchoolYearId,
                sy.name                    AS SchoolYearName,
                e.estado                   AS Status,
                e.created_at               AS CreatedAt
            FROM evaluaciones_psicopedagogicas e
            JOIN "student" s ON s.id = e.alumno_id
            LEFT JOIN "school_year" sy ON sy.id = e.ciclo_id
            WHERE e.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@StudentId IS NULL OR e.alumno_id = @StudentId)
              AND (@CicloId   IS NULL OR e.ciclo_id  = @CicloId)
            ORDER BY e.created_at DESC;
            """;

        return await conn.QueryAsync<EvaluacionListItemDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaId = attentionAreaId,
            StudentId = studentId,
            CicloId = cicloId
        });
    }

    public async Task<IEnumerable<TeaAlertDto>> GetTeaAlerts(
        int[] allowedSchoolIds, int attentionAreaId, int? cicloId, int? alertLevel)
    {
        if (allowedSchoolIds.Length == 0) return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                ts.alumno_id                AS StudentId,
                {StudentNameExpr}           AS StudentName,
                sc.name                     AS SchoolName,
                CASE ts.nivel_alerta
                    WHEN 'SIGNIFICATIVO' THEN 2
                    WHEN 'MODERADO'      THEN 1
                    ELSE 0
                END                         AS AlertLevel,
                ts.fecha                    AS ScreeningDate
            FROM tea_screenings ts
            JOIN "student" s ON s.id = ts.alumno_id
            LEFT JOIN "school" sc ON sc.id = s.school_id
            WHERE ts.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@CicloId IS NULL OR ts.ciclo_id = @CicloId)
              AND (@AlertLevel IS NULL OR
                   CASE ts.nivel_alerta
                       WHEN 'SIGNIFICATIVO' THEN 2
                       WHEN 'MODERADO'      THEN 1
                       ELSE 0
                   END = @AlertLevel)
            ORDER BY ts.fecha DESC;
            """;

        return await conn.QueryAsync<TeaAlertDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaId = attentionAreaId,
            CicloId = cicloId,
            AlertLevel = alertLevel
        });
    }

    public async Task<IEnumerable<CieSummaryDto>> GetCieSummary(
        int[] allowedSchoolIds, int attentionAreaId, Guid? studentId, int? cicloId)
    {
        if (allowedSchoolIds.Length == 0) return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                ce.alumno_id                                                       AS StudentId,
                {StudentNameExpr}                                                  AS StudentName,
                CONCAT('Dimensión ', ce.dimension_id)                              AS DimensionName,
                COUNT(cr.id)                                                       AS TotalIndicators,
                COUNT(cr.id) FILTER (WHERE cr.logrado = TRUE)                      AS CompletedIndicators,
                ROUND(100.0 * COUNT(cr.id) FILTER (WHERE cr.logrado = TRUE)
                      / NULLIF(COUNT(cr.id), 0), 1)                                AS Percentage
            FROM cie_evaluaciones ce
            JOIN "student" s ON s.id = ce.alumno_id
            LEFT JOIN cie_respuestas cr ON cr.evaluacion_id = ce.id
            WHERE ce.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@StudentId IS NULL OR ce.alumno_id = @StudentId)
              AND (@CicloId   IS NULL OR ce.ciclo_id  = @CicloId)
            GROUP BY ce.alumno_id, {StudentNameExpr}, ce.dimension_id
            ORDER BY StudentName;
            """;

        return await conn.QueryAsync<CieSummaryDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaId = attentionAreaId,
            StudentId = studentId,
            CicloId = cicloId
        });
    }
}
