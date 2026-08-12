namespace Back_end_RepostesSAE.Models.Dto;

public sealed class CieSubindicadorDto
{
    public int Id { get; set; }
    public int IndicadorId { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
}

public sealed class CieIndicadorDto
{
    public int Id { get; set; }
    public int DimensionId { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public List<CieSubindicadorDto> Subindicadores { get; set; } = [];
}

public sealed class CieDimensionDto
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public List<CieIndicadorDto> Indicadores { get; set; } = [];
}

public sealed class CieEvaluacionListItemDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public Guid EvaluadorId { get; set; }
    public int CicloId { get; set; }
    public int DimensionId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "EN_PROCESO";
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateCieEvaluacionRequest
{
    public Guid AlumnoId { get; set; }
    public int CicloId { get; set; }
    public int DimensionId { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class CieRespuestaUpsertItem
{
    public int SubindicadorId { get; set; }
    public bool? Logrado { get; set; }
    public short? NivelAyuda { get; set; }
    public string? RespuestaTipo { get; set; }
    public string? Observacion { get; set; }
}

public sealed class UpsertCieRespuestasRequest
{
    public List<CieRespuestaUpsertItem> Items { get; set; } = [];
}

public sealed class CieFonoarticuladorUpsertItem
{
    public int SubindicadorId { get; set; }
    public bool? Funcional { get; set; }
    public string? ObservacionForma { get; set; }
}

public sealed class UpsertCieFonoarticuladorRequest
{
    public List<CieFonoarticuladorUpsertItem> Items { get; set; } = [];
}
