namespace Back_end_RepostesSAE.Models.Dto;

/// <summary>Campos clínicos compartidos entre el detalle y el request de guardado.</summary>
public abstract class EvaluacionCamposBase
{
    public string[] AreasEvaluar { get; set; } = [];
    public string[] InstrumentosAplicar { get; set; } = [];
    public string? InstrumentosDetalle { get; set; }
    public string? MotivoEvaluacion { get; set; }
    public string? ConductaEvaluacion { get; set; }
    public string? AntecedentesEmbarazo { get; set; }
    public string? AntecedentesHeredo { get; set; }
    public string? DesarrolloMotor { get; set; }
    public string? DesarrolloLenguaje { get; set; }
    public string? HistoriaMedica { get; set; }
    public string? HistoriaEscolar { get; set; }
    public string? SituacionFamiliar { get; set; }
    public string? DescripcionAlumno { get; set; }
    public string? ContextoFamiliar { get; set; }
    public string? ContextoEscolar { get; set; }
    public string? ContextoSocial { get; set; }
    public string? DesarrolloFisico { get; set; }
    public string? DesarrolloCognitivo { get; set; }
    public string? DesarrolloSocioafectivo { get; set; }
    public string? EvaluacionAprendizajes { get; set; }
    public string? Creatividad { get; set; }
    public string? InterpretacionResultados { get; set; }
    public string? Conclusiones { get; set; }
}

/// <summary>Resumen de evaluación expuesto al rol TUTOR (sin campos clínicos).</summary>
public sealed class TutorEvaluacionResumenDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public int CicloId { get; set; }
    public DateOnly FechaElaboracion { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public string? MotivoEvaluacion { get; set; }
}

public sealed class EvaluacionDetailDto : EvaluacionCamposBase
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string? StudentName { get; set; }
    public int CicloId { get; set; }
    public DateOnly FechaElaboracion { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SaveEvaluacionRequest : EvaluacionCamposBase
{
    public Guid AlumnoId { get; set; }
    public int CicloId { get; set; }
    public string? Estado { get; set; }
}

public sealed class EvalPsicoBapDto
{
    public int? Id { get; set; }
    public string? TipoBap { get; set; }
    public string? Contexto { get; set; }
    public string? IndicadorInclusion { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class EvalPsicoColaboradorDto
{
    public int? Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? NombreExterno { get; set; }
    public string? RolColaborador { get; set; }
    public bool FirmaDigital { get; set; }
    public DateTime? FechaFirma { get; set; }
}

public sealed class ReplaceEvalPsicoBapRequest
{
    public List<EvalPsicoBapDto> Items { get; set; } = [];
}

public sealed class ReplaceEvalPsicoColaboradoresRequest
{
    public List<EvalPsicoColaboradorDto> Items { get; set; } = [];
}
