using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class CanalizacionRepository(IConfiguration configuration) : ICanalizacionRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<IEnumerable<CanalizacionListItemDto>> GetCanalizaciones(
        int[] allowedSchoolIds, int attentionAreaId,
        string? estado, Guid? solicitanteId, Guid? receptorId)
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
                      WHERE saa.student_id = s.id AND saa.attention_area_id = @AreaId
                  )
            )
            SELECT
                c.id                                                                      AS Id,
                c.alumno_id                                                               AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS AlumnoNombre,
                c.ciclo_id                                                                AS CicloId,
                c.fecha                                                                   AS Fecha,
                c.area_canaliza                                                           AS AreaCanaliza,
                aa.name                                                                   AS AreaNombre,
                c.motivo                                                                  AS Motivo,
                c.acciones_aula                                                           AS AccionesAula,
                c.solicitante_id                                                          AS SolicitanteId,
                TRIM(CONCAT(us.name, ' ', us.father_last_name))                           AS SolicitanteNombre,
                c.receptor_id                                                             AS ReceptorId,
                TRIM(CONCAT(ur.name, ' ', ur.father_last_name))                           AS ReceptorNombre,
                c.fecha_recibido                                                          AS FechaRecibido,
                c.estado                                                                  AS Estado,
                c.created_at                                                              AS CreatedAt
            FROM canalizaciones c
            JOIN "student" s ON s.id = c.alumno_id
            LEFT JOIN "attention_area" aa ON aa.id = c.area_canaliza
            LEFT JOIN "user" us ON us.id = c.solicitante_id
            LEFT JOIN "user" ur ON ur.id = c.receptor_id
            WHERE c.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@Estado IS NULL OR c.estado = @Estado)
              AND (@SolicitanteId IS NULL OR c.solicitante_id = @SolicitanteId)
              AND (@ReceptorId IS NULL OR c.receptor_id = @ReceptorId)
            ORDER BY c.created_at DESC;
            """;

        return await conn.QueryAsync<CanalizacionListItemDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaId = attentionAreaId,
            Estado = string.IsNullOrWhiteSpace(estado) ? null : estado,
            SolicitanteId = solicitanteId,
            ReceptorId = receptorId
        });
    }

    public async Task<int> Create(AddCanalizacionRequest request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO canalizaciones
                (alumno_id, ciclo_id, area_canaliza, motivo, acciones_aula, solicitante_id, receptor_id, estado)
            VALUES
                (@AlumnoId, @CicloId, @AreaCanaliza, @Motivo, @AccionesAula, @SolicitanteId, @ReceptorId, 'PENDIENTE')
            RETURNING id;
            """;

        return await conn.ExecuteScalarAsync<int>(sql, request);
    }

    public async Task<bool> UpdateEstado(int id, string estado)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE canalizaciones
            SET estado = @Estado,
                fecha_recibido = CASE
                    WHEN @Estado = 'RECIBIDA' AND fecha_recibido IS NULL THEN CURRENT_DATE
                    ELSE fecha_recibido
                END
            WHERE id = @Id;
            """;

        var rows = await conn.ExecuteAsync(sql, new { Id = id, Estado = estado });
        return rows > 0;
    }

    public async Task<Guid?> GetAlumnoId(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT alumno_id FROM canalizaciones WHERE id = @Id;";
        return await conn.ExecuteScalarAsync<Guid?>(sql, new { Id = id });
    }
}
