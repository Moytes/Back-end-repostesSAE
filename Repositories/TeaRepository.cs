using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class TeaRepository(IConfiguration configuration) : ITeaRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<int> Create(
        Guid alumnoId, Guid evaluadorId, int cicloId, int puntajeTotal, string nivelAlerta, string? contextoObs)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO tea_screenings
                (alumno_id, evaluador_id, ciclo_id, puntaje_total, nivel_alerta, contexto_obs)
            VALUES (@AlumnoId, @EvaluadorId, @CicloId, @PuntajeTotal, @NivelAlerta, @ContextoObs)
            RETURNING id;
            """;

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            AlumnoId = alumnoId,
            EvaluadorId = evaluadorId,
            CicloId = cicloId,
            PuntajeTotal = puntajeTotal,
            NivelAlerta = nivelAlerta,
            ContextoObs = contextoObs
        });
    }

    public async Task<IEnumerable<TeaScreeningDto>> GetHistory(Guid alumnoId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                id                      AS Id,
                alumno_id               AS AlumnoId,
                evaluador_id            AS EvaluadorId,
                ciclo_id                AS CicloId,
                fecha                   AS Fecha,
                contexto_obs            AS ContextoObs,
                observaciones_generales AS ObservacionesGenerales,
                puntaje_total           AS PuntajeTotal,
                nivel_alerta            AS NivelAlerta,
                requiere_canalizacion   AS RequiereCanalizacion,
                created_at              AS CreatedAt
            FROM tea_screenings
            WHERE alumno_id = @AlumnoId
            ORDER BY fecha DESC;
            """;

        return await conn.QueryAsync<TeaScreeningDto>(sql, new { AlumnoId = alumnoId });
    }

    public async Task<bool> CanConnect()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var result = await conn.ExecuteScalarAsync<int>("SELECT 1;");
        return result == 1;
    }
}
