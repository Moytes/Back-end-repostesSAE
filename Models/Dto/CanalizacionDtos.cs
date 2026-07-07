namespace Back_end_RepostesSAE.Models.Dto;

public sealed class CanalizacionListItemDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public int CicloId { get; set; }
    public DateOnly Fecha { get; set; }
    public int? AreaCanaliza { get; set; }
    public string? AreaNombre { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? AccionesAula { get; set; }
    public Guid? SolicitanteId { get; set; }
    public string? SolicitanteNombre { get; set; }
    public Guid? ReceptorId { get; set; }
    public string? ReceptorNombre { get; set; }
    public DateOnly? FechaRecibido { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime CreatedAt { get; set; }
}

public sealed class AddCanalizacionRequest
{
    public Guid AlumnoId { get; set; }
    public int CicloId { get; set; }
    public int? AreaCanaliza { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? AccionesAula { get; set; }
    public Guid? SolicitanteId { get; set; }
    public Guid? ReceptorId { get; set; }
}

public sealed class UpdateCanalizacionEstadoRequest
{
    public string Estado { get; set; } = string.Empty;
}
