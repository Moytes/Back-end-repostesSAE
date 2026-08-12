namespace Back_end_RepostesSAE.Models.Dto;

public sealed class ExpedienteAlumnoDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Curp { get; set; }
    public string? EscuelaNombre { get; set; }
    public string? Grupo { get; set; }
    public short? Grado { get; set; }
    public List<string> AreasAtencion { get; set; } = [];
    public List<string> Discapacidades { get; set; } = [];
}

public sealed class ExpedienteActividadDto
{
    public int Id { get; set; }
    public string MaterialTitulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaLimite { get; set; }
    public DateTime? FechaCompletado { get; set; }
    public string? Retroalimentacion { get; set; }
    public string? Instrucciones { get; set; }
}

public sealed class ExpedienteDto
{
    public ExpedienteAlumnoDto Alumno { get; set; } = new();
    public List<CanalizacionListItemDto> Canalizaciones { get; set; } = [];
    public List<EvaluacionListItemDto> Evaluaciones { get; set; } = [];
    public List<TeaScreeningDto> TeaHistorial { get; set; } = [];
    public List<CitaListItemDto> Citas { get; set; } = [];
    public List<SesionListItemDto> Sesiones { get; set; } = [];
    public List<CieSummaryDto> CieResumen { get; set; } = [];
    public List<ExpedienteActividadDto> Actividades { get; set; } = [];
}
