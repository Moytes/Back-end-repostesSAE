namespace Back_end_RepostesSAE.Models.Dto;

/// <summary>Campos clínicos compartidos entre el detalle y el request de guardado.</summary>
public abstract class EvaluacionCamposBase
{
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
