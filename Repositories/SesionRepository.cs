using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class SesionRepository(IConfiguration configuration) : ISesionRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<IEnumerable<SesionListItemDto>> GetByStudent(Guid alumnoId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                se.id           AS Id,
                se.alumno_id    AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS AlumnoNombre,
                se.psicologo_id AS PsicologoId,
                se.ciclo_id     AS CicloId,
                se.fecha        AS Fecha,
                se.tipo         AS Tipo,
                se.motivo       AS Motivo,
                se.nota         AS Nota,
                se.acuerdos     AS Acuerdos,
                se.created_at   AS CreatedAt
            FROM sesiones_psicologicas se
            JOIN "student" s ON s.id = se.alumno_id
            WHERE se.alumno_id = @AlumnoId
            ORDER BY se.fecha DESC, se.id DESC;
            """;

        return await conn.QueryAsync<SesionListItemDto>(sql, new { AlumnoId = alumnoId });
    }

    public async Task<int> Create(Guid alumnoId, Guid psicologoId, AddSesionRequest r)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO sesiones_psicologicas (alumno_id, psicologo_id, ciclo_id, fecha, tipo, motivo, nota, acuerdos)
            VALUES (@AlumnoId, @PsicologoId, @CicloId, COALESCE(@Fecha, CURRENT_DATE), @Tipo, @Motivo, @Nota, @Acuerdos)
            RETURNING id;
            """;

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            AlumnoId = alumnoId,
            PsicologoId = psicologoId,
            r.CicloId, r.Fecha, r.Tipo, r.Motivo, r.Nota, r.Acuerdos
        });
    }

    public async Task<bool> Update(int id, UpdateSesionRequest r)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE sesiones_psicologicas SET
                fecha    = COALESCE(@Fecha, fecha),
                tipo     = @Tipo,
                motivo   = @Motivo,
                nota     = @Nota,
                acuerdos = @Acuerdos
            WHERE id = @Id;
            """;

        var rows = await conn.ExecuteAsync(sql, new { Id = id, r.Fecha, r.Tipo, r.Motivo, r.Nota, r.Acuerdos });
        return rows > 0;
    }

    public async Task<bool> Delete(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync("DELETE FROM sesiones_psicologicas WHERE id = @Id;", new { Id = id });
        return rows > 0;
    }

    public async Task<Guid?> GetAlumnoId(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<Guid?>(
            "SELECT alumno_id FROM sesiones_psicologicas WHERE id = @Id;", new { Id = id });
    }
}
