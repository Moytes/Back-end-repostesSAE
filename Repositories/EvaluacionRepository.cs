using Back_end_RepostesSAE.Models.Dto;
using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class EvaluacionRepository(IConfiguration configuration) : IEvaluacionRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<EvaluacionDetailDto?> GetDetail(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                e.id                        AS Id,
                e.alumno_id                 AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS StudentName,
                e.ciclo_id                  AS CicloId,
                e.fecha_elaboracion         AS FechaElaboracion,
                e.motivo_evaluacion         AS MotivoEvaluacion,
                e.conducta_evaluacion       AS ConductaEvaluacion,
                e.antecedentes_embarazo     AS AntecedentesEmbarazo,
                e.antecedentes_heredo       AS AntecedentesHeredo,
                e.desarrollo_motor          AS DesarrolloMotor,
                e.desarrollo_lenguaje       AS DesarrolloLenguaje,
                e.historia_medica           AS HistoriaMedica,
                e.historia_escolar          AS HistoriaEscolar,
                e.situacion_familiar        AS SituacionFamiliar,
                e.descripcion_alumno        AS DescripcionAlumno,
                e.contexto_familiar         AS ContextoFamiliar,
                e.contexto_escolar          AS ContextoEscolar,
                e.contexto_social           AS ContextoSocial,
                e.desarrollo_fisico         AS DesarrolloFisico,
                e.desarrollo_cognitivo      AS DesarrolloCognitivo,
                e.desarrollo_socioafectivo  AS DesarrolloSocioafectivo,
                e.evaluacion_aprendizajes   AS EvaluacionAprendizajes,
                e.creatividad               AS Creatividad,
                e.interpretacion_resultados AS InterpretacionResultados,
                e.conclusiones              AS Conclusiones,
                e.estado                    AS Estado,
                e.created_at                AS CreatedAt,
                e.updated_at                AS UpdatedAt
            FROM evaluaciones_psicopedagogicas e
            JOIN "student" s ON s.id = e.alumno_id
            WHERE e.id = @Id;
            """;

        return await conn.QueryFirstOrDefaultAsync<EvaluacionDetailDto>(sql, new { Id = id });
    }

    public async Task<IEnumerable<EvaluacionDetailDto>> GetByStudent(Guid studentId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT
                e.id                        AS Id,
                e.alumno_id                 AS AlumnoId,
                TRIM(CONCAT(s.name, ' ', s.father_last_name, ' ', COALESCE(s.mother_last_name, ''))) AS StudentName,
                e.ciclo_id                  AS CicloId,
                e.fecha_elaboracion         AS FechaElaboracion,
                e.motivo_evaluacion         AS MotivoEvaluacion,
                e.conducta_evaluacion       AS ConductaEvaluacion,
                e.antecedentes_embarazo     AS AntecedentesEmbarazo,
                e.antecedentes_heredo       AS AntecedentesHeredo,
                e.desarrollo_motor          AS DesarrolloMotor,
                e.desarrollo_lenguaje       AS DesarrolloLenguaje,
                e.historia_medica           AS HistoriaMedica,
                e.historia_escolar          AS HistoriaEscolar,
                e.situacion_familiar        AS SituacionFamiliar,
                e.descripcion_alumno        AS DescripcionAlumno,
                e.contexto_familiar         AS ContextoFamiliar,
                e.contexto_escolar          AS ContextoEscolar,
                e.contexto_social           AS ContextoSocial,
                e.desarrollo_fisico         AS DesarrolloFisico,
                e.desarrollo_cognitivo      AS DesarrolloCognitivo,
                e.desarrollo_socioafectivo  AS DesarrolloSocioafectivo,
                e.evaluacion_aprendizajes   AS EvaluacionAprendizajes,
                e.creatividad               AS Creatividad,
                e.interpretacion_resultados AS InterpretacionResultados,
                e.conclusiones              AS Conclusiones,
                e.estado                    AS Estado,
                e.created_at                AS CreatedAt,
                e.updated_at                AS UpdatedAt
            FROM evaluaciones_psicopedagogicas e
            JOIN "student" s ON s.id = e.alumno_id
            WHERE e.alumno_id = @StudentId
            ORDER BY e.fecha_elaboracion DESC;
            """;

        return await conn.QueryAsync<EvaluacionDetailDto>(sql, new { StudentId = studentId });
    }

    public async Task<Guid?> GetAlumnoId(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<Guid?>(
            "SELECT alumno_id FROM evaluaciones_psicopedagogicas WHERE id = @Id;", new { Id = id });
    }

    public async Task<int> Create(SaveEvaluacionRequest r)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO evaluaciones_psicopedagogicas (
                alumno_id, ciclo_id, estado,
                motivo_evaluacion, conducta_evaluacion, antecedentes_embarazo, antecedentes_heredo,
                desarrollo_motor, desarrollo_lenguaje, historia_medica, historia_escolar,
                situacion_familiar, descripcion_alumno, contexto_familiar, contexto_escolar,
                contexto_social, desarrollo_fisico, desarrollo_cognitivo, desarrollo_socioafectivo,
                evaluacion_aprendizajes, creatividad, interpretacion_resultados, conclusiones
            ) VALUES (
                @AlumnoId, @CicloId, COALESCE(@Estado, 'BORRADOR'),
                @MotivoEvaluacion, @ConductaEvaluacion, @AntecedentesEmbarazo, @AntecedentesHeredo,
                @DesarrolloMotor, @DesarrolloLenguaje, @HistoriaMedica, @HistoriaEscolar,
                @SituacionFamiliar, @DescripcionAlumno, @ContextoFamiliar, @ContextoEscolar,
                @ContextoSocial, @DesarrolloFisico, @DesarrolloCognitivo, @DesarrolloSocioafectivo,
                @EvaluacionAprendizajes, @Creatividad, @InterpretacionResultados, @Conclusiones
            )
            RETURNING id;
            """;

        return await conn.ExecuteScalarAsync<int>(sql, r);
    }

    public async Task<bool> Update(int id, SaveEvaluacionRequest r)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE evaluaciones_psicopedagogicas SET
                estado = COALESCE(@Estado, estado),
                motivo_evaluacion = @MotivoEvaluacion,
                conducta_evaluacion = @ConductaEvaluacion,
                antecedentes_embarazo = @AntecedentesEmbarazo,
                antecedentes_heredo = @AntecedentesHeredo,
                desarrollo_motor = @DesarrolloMotor,
                desarrollo_lenguaje = @DesarrolloLenguaje,
                historia_medica = @HistoriaMedica,
                historia_escolar = @HistoriaEscolar,
                situacion_familiar = @SituacionFamiliar,
                descripcion_alumno = @DescripcionAlumno,
                contexto_familiar = @ContextoFamiliar,
                contexto_escolar = @ContextoEscolar,
                contexto_social = @ContextoSocial,
                desarrollo_fisico = @DesarrolloFisico,
                desarrollo_cognitivo = @DesarrolloCognitivo,
                desarrollo_socioafectivo = @DesarrolloSocioafectivo,
                evaluacion_aprendizajes = @EvaluacionAprendizajes,
                creatividad = @Creatividad,
                interpretacion_resultados = @InterpretacionResultados,
                conclusiones = @Conclusiones
            WHERE id = @Id;
            """;

        var rows = await conn.ExecuteAsync(sql, new
        {
            Id = id,
            r.Estado,
            r.MotivoEvaluacion, r.ConductaEvaluacion, r.AntecedentesEmbarazo, r.AntecedentesHeredo,
            r.DesarrolloMotor, r.DesarrolloLenguaje, r.HistoriaMedica, r.HistoriaEscolar,
            r.SituacionFamiliar, r.DescripcionAlumno, r.ContextoFamiliar, r.ContextoEscolar,
            r.ContextoSocial, r.DesarrolloFisico, r.DesarrolloCognitivo, r.DesarrolloSocioafectivo,
            r.EvaluacionAprendizajes, r.Creatividad, r.InterpretacionResultados, r.Conclusiones
        });
        return rows > 0;
    }
}
