using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class TeaRepository(IConfiguration configuration) : ITeaRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    private static readonly IReadOnlyList<TeaIndicadorDto> FallbackIndicadores =
    [
        new() { Id = 1, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_01", Descripcion = "Dificultad para iniciar o mantener conversaciones", Orden = 1 },
        new() { Id = 2, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_02", Descripcion = "Respuestas inusuales en interacciones sociales", Orden = 2 },
        new() { Id = 3, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_03", Descripcion = "Contacto visual limitado o atípico", Orden = 3 },
        new() { Id = 4, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_04", Descripcion = "Dificultad para comprender lenguaje no literal (ironía, chistes)", Orden = 4 },
        new() { Id = 5, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_05", Descripcion = "Dificultad para hacer amigos o mantener relaciones", Orden = 5 },
        new() { Id = 6, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_06", Descripcion = "Expresión emocional limitada o inadecuada al contexto", Orden = 6 },
        new() { Id = 7, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_07", Descripcion = "Dificultad para tomar turnos en la conversación", Orden = 7 },
        new() { Id = 8, Dominio = "COMUNICACION_SOCIAL", Codigo = "TEA_CS_08", Descripcion = "Prosodia inusual (tono monótono, volumen inadecuado)", Orden = 8 },
        new() { Id = 9, Dominio = "CONDUCTA_REPETITIVA", Codigo = "TEA_CR_01", Descripcion = "Intereses intensos y restringidos", Orden = 1 },
        new() { Id = 10, Dominio = "CONDUCTA_REPETITIVA", Codigo = "TEA_CR_02", Descripcion = "Inflexibilidad ante cambios de rutina", Orden = 2 },
        new() { Id = 11, Dominio = "CONDUCTA_REPETITIVA", Codigo = "TEA_CR_03", Descripcion = "Movimientos repetitivos o estereotipados", Orden = 3 },
        new() { Id = 12, Dominio = "CONDUCTA_REPETITIVA", Codigo = "TEA_CR_04", Descripcion = "Hiper o hipo reactividad sensorial", Orden = 4 },
        new() { Id = 13, Dominio = "CONDUCTA_REPETITIVA", Codigo = "TEA_CR_05", Descripcion = "Adherencia excesiva a reglas o patrones", Orden = 5 }
    ];

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

    public async Task<int> CreateWithRespuestas(
        Guid alumnoId, Guid evaluadorId, int cicloId, string? contextoObs, IReadOnlyList<TeaRespuestaItem> respuestas)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string insertScreening = """
            INSERT INTO tea_screenings
                (alumno_id, evaluador_id, ciclo_id, puntaje_total, nivel_alerta, contexto_obs)
            VALUES (@AlumnoId, @EvaluadorId, @CicloId, 0, 'SIN_ALERTA', @ContextoObs)
            RETURNING id;
            """;

        var screeningId = await conn.ExecuteScalarAsync<int>(insertScreening, new
        {
            AlumnoId = alumnoId,
            EvaluadorId = evaluadorId,
            CicloId = cicloId,
            ContextoObs = contextoObs
        }, tx);

        await InsertRespuestas(conn, tx, screeningId, respuestas);
        await tx.CommitAsync();
        return screeningId;
    }

    public async Task SaveRespuestas(int screeningId, IReadOnlyList<TeaRespuestaItem> respuestas)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await InsertRespuestas(conn, tx, screeningId, respuestas);
        await tx.CommitAsync();
    }

    public async Task<(int PuntajeTotal, string NivelAlerta)?> GetScreeningScore(int screeningId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT puntaje_total AS PuntajeTotal, nivel_alerta AS NivelAlerta
            FROM tea_screenings WHERE id = @Id;
            """;
        var row = await conn.QueryFirstOrDefaultAsync<(short? PuntajeTotal, string? NivelAlerta)>(sql, new { Id = screeningId });
        if (row.NivelAlerta == null)
            return null;
        return (row.PuntajeTotal ?? 0, row.NivelAlerta);
    }

    public async Task<IReadOnlyList<TeaIndicadorDto>> GetIndicadores()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            const string sql = """
                SELECT
                    id          AS Id,
                    dominio     AS Dominio,
                    codigo      AS Codigo,
                    descripcion AS Descripcion,
                    orden       AS Orden
                FROM cat_tea_indicadores
                ORDER BY orden, id;
                """;
            var rows = (await conn.QueryAsync<TeaIndicadorDto>(sql)).ToList();
            return rows.Count > 0 ? rows : FallbackIndicadores;
        }
        catch (PostgresException)
        {
            return FallbackIndicadores;
        }
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

    public async Task<Guid?> GetAlumnoId(int screeningId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<Guid?>(
            "SELECT alumno_id FROM tea_screenings WHERE id = @Id;", new { Id = screeningId });
    }

    public async Task<bool> UpdateSeguimiento(int screeningId, string estado, string? nota)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE tea_screenings
            SET seguimiento_estado = @Estado,
                seguimiento_at = NOW(),
                seguimiento_nota = @Nota
            WHERE id = @Id;
            """;
        var rows = await conn.ExecuteAsync(sql, new { Id = screeningId, Estado = estado, Nota = nota });
        return rows > 0;
    }

    public async Task<bool> CanConnect()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var result = await conn.ExecuteScalarAsync<int>("SELECT 1;");
        return result == 1;
    }

    private static async Task InsertRespuestas(
        NpgsqlConnection conn, NpgsqlTransaction tx, int screeningId, IReadOnlyList<TeaRespuestaItem> respuestas)
    {
        const string sql = """
            INSERT INTO tea_respuestas (screening_id, indicador_id, frecuencia, intensidad, observacion)
            VALUES (@ScreeningId, @IndicadorId, @Frecuencia, @Intensidad, @Observacion)
            ON CONFLICT (screening_id, indicador_id) DO UPDATE SET
                frecuencia = EXCLUDED.frecuencia,
                intensidad = EXCLUDED.intensidad,
                observacion = EXCLUDED.observacion;
            """;

        foreach (var item in respuestas)
        {
            await conn.ExecuteAsync(sql, new
            {
                ScreeningId = screeningId,
                item.IndicadorId,
                item.Frecuencia,
                item.Intensidad,
                item.Observacion
            }, tx);
        }
    }
}
