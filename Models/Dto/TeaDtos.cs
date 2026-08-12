namespace Back_end_RepostesSAE.Models.Dto;

public sealed class TeaIndicadorDto
{
    public int Id { get; init; }
    public string Dominio { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public int Orden { get; init; }
}

public sealed class TeaRespuestaItem
{
    public int IndicadorId { get; init; }
    public short Frecuencia { get; init; }
    public short Intensidad { get; init; }
    public string? Observacion { get; init; }
}

public sealed class TeaEvaluationRequest
{
    public Guid StudentId { get; init; }
    public int CicloId { get; init; }
    public List<int> Answers { get; init; } = [];
    public List<TeaRespuestaItem>? Respuestas { get; init; }
    public string? ContextoObs { get; init; }
}

public sealed class TeaScoringResultDto
{
    public int TotalScore { get; init; }
    public string AlertLevel { get; init; } = string.Empty;
}

public sealed class TeaScreeningDto
{
    public int Id { get; set; }
    public Guid AlumnoId { get; set; }
    public Guid EvaluadorId { get; set; }
    public int CicloId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? ContextoObs { get; set; }
    public string? ObservacionesGenerales { get; set; }
    public short? PuntajeTotal { get; set; }
    public string? NivelAlerta { get; set; }
    public bool RequiereCanalizacion { get; set; }
    public DateTime CreatedAt { get; set; }
}
