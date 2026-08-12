namespace Back_end_RepostesSAE.Models.Dto;

public sealed class AgendarCitaRequest
{
    public Guid AlumnoId { get; set; }
    public int? TeaScreeningId { get; set; }
    public string TipoCita { get; set; } = string.Empty;
    public DateOnly? Fecha { get; set; }
    public TimeOnly? Hora { get; set; }
    public string Modalidad { get; set; } = "PRESENCIAL";
    public string? NotasTutor { get; set; }
}

public sealed class AgendarCitaResultDto
{
    public int Id { get; set; }
}

/// <summary>Listado de citas para el portal del tutor (sin notas clínicas).</summary>
public sealed class TutorCitaListItemDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string TipoCita { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string Estado { get; set; } = "PROGRAMADA";
}

public sealed class CitaListItemDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public string TipoCita { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string Estado { get; set; } = "PROGRAMADA";
    public int? TeaScreeningId { get; set; }
    public string? NotasTutor { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class UpdateCitaEstadoRequest
{
    public string Estado { get; set; } = string.Empty;
}
