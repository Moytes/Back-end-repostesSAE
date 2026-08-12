using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class CitaRepository(IConfiguration configuration) : ICitaRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<AgendarCitaResultDto> Create(Guid especialistaId, AgendarCitaRequest request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string conflictSql = """
            SELECT EXISTS (
                SELECT 1 FROM citas_especialista
                WHERE especialista_id = @EspecialistaId
                  AND fecha = @Fecha
                  AND hora = @Hora
                  AND estado <> 'CANCELADA'
            );
            """;
        var hasConflict = await conn.ExecuteScalarAsync<bool>(conflictSql, new
        {
            EspecialistaId = especialistaId,
            request.Fecha,
            request.Hora
        });
        if (hasConflict)
            throw new InvalidOperationException("Ya tienes una cita programada en esa fecha y hora.");

        const string appointmentSql = """
            INSERT INTO citas_especialista
                (alumno_id, especialista_id, tea_screening_id, tipo_cita, fecha, hora,
                 modalidad, notas_tutor)
            SELECT
                @AlumnoId, @EspecialistaId, @TeaScreeningId, @TipoCita, @Fecha, @Hora,
                @Modalidad, @NotasTutor
            WHERE @TeaScreeningId IS NULL
               OR EXISTS (
                   SELECT 1
                   FROM tea_screenings
                   WHERE id = @TeaScreeningId AND alumno_id = @AlumnoId
               )
            RETURNING id;
            """;

        var appointmentId = await conn.ExecuteScalarAsync<int?>(appointmentSql, new
        {
            request.AlumnoId,
            EspecialistaId = especialistaId,
            request.TeaScreeningId,
            request.TipoCita,
            request.Fecha,
            request.Hora,
            request.Modalidad,
            request.NotasTutor
        });

        if (appointmentId == null)
            throw new InvalidOperationException("La alerta TEA no corresponde al alumno seleccionado.");

        return new AgendarCitaResultDto { Id = appointmentId.Value };
    }

    public async Task<IEnumerable<CitaListItemDto>> List(
        int[] allowedSchoolIds, int[] attentionAreaIds,
        DateOnly? from, DateOnly? to, Guid? alumnoId)
    {
        if (allowedSchoolIds.Length == 0)
            return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
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
            SELECT
                c.id                AS Id,
                c.alumno_id         AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS AlumnoNombre,
                c.tipo_cita         AS TipoCita,
                c.fecha             AS Fecha,
                c.hora              AS Hora,
                c.modalidad         AS Modalidad,
                c.estado            AS Estado,
                c.tea_screening_id  AS TeaScreeningId,
                c.notas_tutor       AS NotasTutor,
                c.created_at        AS CreatedAt
            FROM citas_especialista c
            JOIN "student" s ON s.id = c.alumno_id
            WHERE c.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@AlumnoId IS NULL OR c.alumno_id = @AlumnoId)
              AND (@From::date IS NULL OR c.fecha >= @From::date)
              AND (@To::date IS NULL OR c.fecha <= @To::date)
            ORDER BY c.fecha, c.hora, c.id;
            """;

        return await conn.QueryAsync<CitaListItemDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            From = from,
            To = to,
            AlumnoId = alumnoId
        });
    }

    public async Task<IEnumerable<TutorCitaListItemDto>> ListByAlumno(
        Guid alumnoId, DateOnly? from = null, DateOnly? to = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                c.id        AS Id,
                c.alumno_id AS AlumnoId,
                c.tipo_cita AS TipoCita,
                c.fecha     AS Fecha,
                c.hora      AS Hora,
                c.modalidad AS Modalidad,
                c.estado    AS Estado
            FROM citas_especialista c
            WHERE c.alumno_id = @AlumnoId
              AND (@From::date IS NULL OR c.fecha >= @From::date)
              AND (@To::date IS NULL OR c.fecha <= @To::date)
            ORDER BY c.fecha, c.hora, c.id;
            """;

        return await conn.QueryAsync<TutorCitaListItemDto>(sql, new
        {
            AlumnoId = alumnoId,
            From = from,
            To = to
        });
    }

    public async Task<bool> UpdateEstado(int id, string estado)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE citas_especialista SET estado = @Estado WHERE id = @Id;
            """;
        var rows = await conn.ExecuteAsync(sql, new { Id = id, Estado = estado });
        return rows > 0;
    }

    public async Task<Guid?> GetAlumnoId(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<Guid?>(
            "SELECT alumno_id FROM citas_especialista WHERE id = @Id;", new { Id = id });
    }
}
