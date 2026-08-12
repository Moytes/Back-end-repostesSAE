using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class CieRepository(IConfiguration configuration) : ICieRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

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

    public async Task<IReadOnlyList<CieDimensionDto>> GetDimensionesCatalogo()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            var tableExists = await conn.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'cie_dimensiones'
                );
                """);
            if (!tableExists)
                return [];

            var dimensiones = (await conn.QueryAsync<CieDimensionDto>(
                """
                SELECT id AS Id, clave AS Clave, nombre AS Nombre, color_hex AS ColorHex,
                       descripcion AS Descripcion, orden AS Orden
                FROM cie_dimensiones
                ORDER BY orden, id;
                """)).ToList();

            var indicadores = (await conn.QueryAsync<CieIndicadorDto>(
                """
                SELECT id AS Id, dimension_id AS DimensionId, codigo AS Codigo, nombre AS Nombre,
                       descripcion AS Descripcion, orden AS Orden
                FROM cie_indicadores
                ORDER BY orden, id;
                """)).ToList();

            var subindicadores = (await conn.QueryAsync<CieSubindicadorDto>(
                """
                SELECT id AS Id, indicador_id AS IndicadorId, codigo AS Codigo, nombre AS Nombre,
                       descripcion AS Descripcion, orden AS Orden
                FROM cie_subindicadores
                ORDER BY orden, id;
                """)).ToList();

            var subsByIndicador = subindicadores.GroupBy(s => s.IndicadorId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var ind in indicadores)
                ind.Subindicadores = subsByIndicador.GetValueOrDefault(ind.Id) ?? [];

            var indByDimension = indicadores.GroupBy(i => i.DimensionId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var dim in dimensiones)
                dim.Indicadores = indByDimension.GetValueOrDefault(dim.Id) ?? [];

            return dimensiones;
        }
        catch (PostgresException)
        {
            return [];
        }
    }

    public async Task<IEnumerable<CieEvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? schoolYearId, int? dimensionId)
    {
        if (allowedSchoolIds.Length == 0)
            return [];

        await using var conn = new NpgsqlConnection(_connectionString);
        var sql = $"""
            {AlumnosScopeCte}
            SELECT
                ce.id           AS Id,
                ce.alumno_id    AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS AlumnoNombre,
                ce.evaluador_id AS EvaluadorId,
                ce.ciclo_id     AS CicloId,
                ce.dimension_id AS DimensionId,
                ce.fecha        AS Fecha,
                ce.observaciones AS Observaciones,
                ce.estado       AS Estado,
                ce.created_at   AS CreatedAt
            FROM cie_evaluaciones ce
            JOIN "student" s ON s.id = ce.alumno_id
            WHERE ce.alumno_id IN (SELECT id FROM alumnos_scope)
              AND (@StudentId IS NULL OR ce.alumno_id = @StudentId)
              AND (@SchoolYearId IS NULL OR ce.ciclo_id = @SchoolYearId)
              AND (@DimensionId IS NULL OR ce.dimension_id = @DimensionId)
            ORDER BY ce.fecha DESC, ce.id DESC;
            """;

        return await conn.QueryAsync<CieEvaluacionListItemDto>(sql, new
        {
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds,
            StudentId = studentId,
            SchoolYearId = schoolYearId,
            DimensionId = dimensionId
        });
    }

    public async Task<int> CreateEvaluacion(Guid evaluadorId, CreateCieEvaluacionRequest request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO cie_evaluaciones (alumno_id, evaluador_id, ciclo_id, dimension_id, observaciones)
            VALUES (@AlumnoId, @EvaluadorId, @CicloId, @DimensionId, @Observaciones)
            RETURNING id;
            """;
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            request.AlumnoId,
            EvaluadorId = evaluadorId,
            request.CicloId,
            request.DimensionId,
            request.Observaciones
        });
    }

    public async Task<Guid?> GetEvaluacionAlumnoId(int evaluacionId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<Guid?>(
            "SELECT alumno_id FROM cie_evaluaciones WHERE id = @Id;", new { Id = evaluacionId });
    }

    public async Task UpsertRespuestas(int evaluacionId, IReadOnlyList<CieRespuestaUpsertItem> items)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string sql = """
            INSERT INTO cie_respuestas
                (evaluacion_id, subindicador_id, logrado, nivel_ayuda, respuesta_tipo, observacion)
            VALUES
                (@EvaluacionId, @SubindicadorId, @Logrado, @NivelAyuda, @RespuestaTipo, @Observacion)
            ON CONFLICT (evaluacion_id, subindicador_id) DO UPDATE SET
                logrado = EXCLUDED.logrado,
                nivel_ayuda = EXCLUDED.nivel_ayuda,
                respuesta_tipo = EXCLUDED.respuesta_tipo,
                observacion = EXCLUDED.observacion;
            """;

        foreach (var item in items)
        {
            await conn.ExecuteAsync(sql, new
            {
                EvaluacionId = evaluacionId,
                item.SubindicadorId,
                item.Logrado,
                item.NivelAyuda,
                RespuestaTipo = item.RespuestaTipo?.Trim().ToUpperInvariant(),
                item.Observacion
            }, tx);
        }

        await tx.CommitAsync();
    }

    public async Task UpsertFonoarticulador(int evaluacionId, IReadOnlyList<CieFonoarticuladorUpsertItem> items)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string sql = """
            INSERT INTO cie_resp_fonoarticulador
                (evaluacion_id, subindicador_id, funcional, observacion_forma)
            VALUES
                (@EvaluacionId, @SubindicadorId, @Funcional, @ObservacionForma)
            ON CONFLICT (evaluacion_id, subindicador_id) DO UPDATE SET
                funcional = EXCLUDED.funcional,
                observacion_forma = EXCLUDED.observacion_forma;
            """;

        foreach (var item in items)
        {
            await conn.ExecuteAsync(sql, new
            {
                EvaluacionId = evaluacionId,
                item.SubindicadorId,
                item.Funcional,
                item.ObservacionForma
            }, tx);
        }

        await tx.CommitAsync();
    }
}
