namespace Back_end_RepostesSAE.Models.Dto;

public sealed class SesionListItemDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string? AlumnoNombre { get; set; }
    public Guid PsicologoId { get; set; }
    public int CicloId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Tipo { get; set; }
    public string? Motivo { get; set; }
    public string Nota { get; set; } = string.Empty;
    public string? Acuerdos { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AddSesionRequest
{
    public int CicloId { get; set; }
    public DateOnly? Fecha { get; set; }
    public string? Tipo { get; set; }
    public string? Motivo { get; set; }
    public string Nota { get; set; } = string.Empty;
    public string? Acuerdos { get; set; }
}

public sealed class UpdateSesionRequest
{
    public DateOnly? Fecha { get; set; }
    public string? Tipo { get; set; }
    public string? Motivo { get; set; }
    public string Nota { get; set; } = string.Empty;
    public string? Acuerdos { get; set; }
}
