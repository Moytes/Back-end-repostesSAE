using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class ClinicalReadRepository(IConfiguration configuration) : IClinicalReadRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    // CTE: alumnos dentro del alcance (escuela permitida + áreas de atención del especialista).
    private const string AlumnosScopeCte = """
        WITH alumnos_scope AS (
            SELECT DISTINCT s.id
            FROM "student" s
            LEFT JOIN "registration" r ON r.student_id = s.id
            LEFT JOIN "group" g ON g.id = r.group_id
            WHERE COALESCE(g.school_id, s.school_id) = ANY(@AllowedSchoolIds)
              AND EXISTS (
                  SELECT 1 FROM "student_attention_area" saa
                  WHERE saa.student_id = s.id AND saa.attention_area_id = ANY(@AreaIds)
              )
        )
        """;

    private const string StudentNameExpr =
        "TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, '')))";

    public async Task<IEnumerable<EvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? cicloId)
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
            AreaIds = attentionAreaIds,
            StudentId = studentId,
            CicloId = cicloId
        });
    }

    public async Task<IEnumerable<TeaAlertDto>> GetTeaAlerts(
        int[] allowedSchoolIds, int[] attentionAreaIds, int? cicloId, int? schoolId, int? alertLevel)
    {
        if (allowedSchoolIds.Length == 0) return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                ts.id                       AS Id,
                ts.alumno_id                AS StudentId,
                {StudentNameExpr}           AS StudentName,
                COALESCE(enr.school_name, sc.name) AS SchoolName,
                enr.grade                   AS Grade,
                enr.group_name              AS GroupName,
                CASE ts.nivel_alerta
                    WHEN 'SIGNIFICATIVO' THEN 2
                    WHEN 'MODERADO'      THEN 1
                    WHEN 'LEVE'          THEN 0
                    ELSE 0
                END                         AS AlertLevel,
                ts.fecha                    AS ScreeningDate,
                ts.created_at               AS CreatedAt,
                ts.contexto_obs             AS ContextoObs,
                ts.observaciones_generales  AS Observaciones,
                ts.requiere_canalizacion    AS RequiereCanalizacion,
                ts.puntaje_total            AS PuntajeTotal,
                COALESCE(ts.seguimiento_estado, 'ACTIVA') AS SeguimientoEstado,
                ts.seguimiento_at           AS SeguimientoAt,
                ts.seguimiento_nota         AS SeguimientoNota
            FROM tea_screenings ts
            JOIN "student" s ON s.id = ts.alumno_id
            LEFT JOIN "school" sc ON sc.id = s.school_id
            LEFT JOIN LATERAL (
                SELECT
                    gr.numero                              AS grade,
                    CONCAT(gr.numero, '° ', g.section)     AS group_name,
                    sc2.name                               AS school_name,
                    sc2.id                                 AS school_id
                FROM "registration" r
                JOIN "group" g ON g.id = r.group_id
                JOIN "grade" gr ON gr.id = g.grade_id
                JOIN "school" sc2 ON sc2.id = g.school_id
                WHERE r.student_id = s.id
                  AND (@CicloId IS NULL OR r.school_year_id = @CicloId)
                ORDER BY r.school_year_id DESC NULLS LAST
                LIMIT 1
            ) enr ON TRUE
            WHERE ts.alumno_id IN (SELECT id FROM alumnos_scope)
              AND ts.nivel_alerta IN ('LEVE', 'MODERADO', 'SIGNIFICATIVO')
              AND (@CicloId IS NULL OR ts.ciclo_id = @CicloId)
              AND (@SchoolId IS NULL OR COALESCE(enr.school_id, s.school_id) = @SchoolId)
              AND (@AlertLevel IS NULL OR
                   CASE ts.nivel_alerta
                       WHEN 'SIGNIFICATIVO' THEN 2
                       WHEN 'MODERADO'      THEN 1
                       WHEN 'LEVE'          THEN 0
                       ELSE -1
                   END = @AlertLevel)
            ORDER BY
                CASE COALESCE(ts.seguimiento_estado, 'ACTIVA')
                    WHEN 'RESUELTA' THEN 1
                    ELSE 0
                END,
                CASE ts.nivel_alerta
                    WHEN 'SIGNIFICATIVO' THEN 2
                    WHEN 'MODERADO'      THEN 1
                    WHEN 'LEVE'          THEN 0
                    ELSE -1
                END DESC,
                ts.fecha DESC,
                ts.id DESC;
            """;

        return await conn.QueryAsync<TeaAlertDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            CicloId = cicloId,
            SchoolId = schoolId,
            AlertLevel = alertLevel
        });
    }

    public async Task<IEnumerable<CieSummaryDto>> GetCieSummary(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? cicloId, int? schoolId = null)
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
            LEFT JOIN LATERAL (
                SELECT sc2.id AS school_id
                FROM "registration" r
                JOIN "group" g ON g.id = r.group_id
                JOIN "school" sc2 ON sc2.id = g.school_id
                WHERE r.student_id = s.id
                  AND (@CicloId IS NULL OR r.school_year_id = @CicloId)
                ORDER BY r.school_year_id DESC NULLS LAST
                LIMIT 1
            ) enr ON TRUE
            WHERE ce.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@StudentId IS NULL OR ce.alumno_id = @StudentId)
              AND (@CicloId   IS NULL OR ce.ciclo_id  = @CicloId)
              AND (@SchoolId  IS NULL OR COALESCE(enr.school_id, s.school_id) = @SchoolId)
            GROUP BY ce.alumno_id, {StudentNameExpr}, ce.dimension_id
            ORDER BY StudentName;
            """;

        return await conn.QueryAsync<CieSummaryDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            StudentId = studentId,
            CicloId = cicloId,
            SchoolId = schoolId
        });
    }

    public async Task<IEnumerable<StudentDataSheetDto>> GetStudentDataSheet(
        int[] allowedSchoolIds, int[] attentionAreaIds, int? schoolId, int? schoolYearId)
    {
        if (allowedSchoolIds.Length == 0) return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                s.id                                    AS StudentId,
                {StudentNameExpr}                        AS StudentName,
                sc.name                                  AS SchoolName,
                CONCAT(gr.numero, '° ', g.section)       AS GroupName,
                gr.numero                                AS Grade,
                COALESCE((
                    SELECT array_agg(DISTINCT d.name)
                    FROM student_disability sd
                    JOIN disability d ON d.id = sd.disability_id
                    WHERE sd.student_id = s.id
                ), ARRAY[]::text[])                      AS Disabilities,
                COALESCE((
                    SELECT array_agg(DISTINCT aa.name)
                    FROM student_attention_area saa2
                    JOIN attention_area aa ON aa.id = saa2.attention_area_id
                    WHERE saa2.student_id = s.id
                ), ARRAY[]::text[])                      AS AttentionAreas,
                (
                    SELECT ce.estado FROM cie_evaluaciones ce
                    WHERE ce.alumno_id = s.id
                    ORDER BY ce.fecha DESC, ce.id DESC
                    LIMIT 1
                )                                        AS CieStatus,
                (
                    SELECT CASE ts.nivel_alerta
                        WHEN 'SIGNIFICATIVO' THEN 2
                        WHEN 'MODERADO' THEN 1
                        ELSE 0
                    END
                    FROM tea_screenings ts
                    WHERE ts.alumno_id = s.id
                    ORDER BY ts.fecha DESC, ts.id DESC
                    LIMIT 1
                )                                        AS TeaAlertLevel
            FROM "student" s
            JOIN "registration" r ON r.student_id = s.id
            JOIN "group" g ON g.id = r.group_id
            JOIN "grade" gr ON gr.id = g.grade_id
            JOIN "school" sc ON sc.id = g.school_id
            WHERE s.id IN (SELECT id FROM alumnos_scope)
              AND (@SchoolId IS NULL OR sc.id = @SchoolId)
              AND (@SchoolYearId IS NULL OR r.school_year_id = @SchoolYearId)
            ORDER BY StudentName;
            """;

        return await conn.QueryAsync<StudentDataSheetDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            SchoolId = schoolId,
            SchoolYearId = schoolYearId
        });
    }

    public async Task<IEnumerable<CanalizacionMonthCountDto>> GetCanalizacionCountsForMonth(
        int[] allowedSchoolIds, int[] attentionAreaIds, int year, int month, int? schoolYearId)
    {
        if (allowedSchoolIds.Length == 0) return [];

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT c.estado AS Estado, COUNT(*)::int AS Total
            FROM canalizaciones c
            WHERE c.alumno_id IN (SELECT id FROM alumnos_scope)
              AND c.fecha >= @Start AND c.fecha < @End
              AND (@SchoolYearId IS NULL OR c.ciclo_id = @SchoolYearId)
            GROUP BY c.estado
            ORDER BY c.estado;
            """;

        return await conn.QueryAsync<CanalizacionMonthCountDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            Start = start,
            End = end,
            SchoolYearId = schoolYearId
        });
    }
}
