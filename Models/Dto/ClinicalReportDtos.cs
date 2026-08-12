namespace Back_end_RepostesSAE.Models.Dto;

public sealed class EvaluacionListItemDto
{
    public int Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int SchoolYearId { get; set; }
    public string? SchoolYearName { get; set; }
    public string Status { get; set; } = "BORRADOR";
    public DateTime CreatedAt { get; set; }
}

public sealed class TeaAlertDto
{
    public int Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? SchoolName { get; set; }
    public short? Grade { get; set; }
    public string? GroupName { get; set; }
    public int AlertLevel { get; set; }
    public DateOnly ScreeningDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ContextoObs { get; set; }
    public string? Observaciones { get; set; }
    public bool RequiereCanalizacion { get; set; }
    public short? PuntajeTotal { get; set; }
    public string SeguimientoEstado { get; set; } = "ACTIVA";
    public DateTime? SeguimientoAt { get; set; }
    public string? SeguimientoNota { get; set; }
}

public sealed class UpdateTeaSeguimientoRequest
{
    public string Estado { get; set; } = string.Empty;
    public string? Nota { get; set; }
}

public sealed class CieSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DimensionName { get; set; } = string.Empty;
    public int TotalIndicators { get; set; }
    public int CompletedIndicators { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class StudentDataSheetDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? SchoolName { get; set; }
    public string? GroupName { get; set; }
    public short? Grade { get; set; }
    // string[] y no List<string>: la consulta usa array_agg(...), que Npgsql/Dapper
    // devuelve como arreglo nativo de Postgres — mapearlo a List<string> tronaba con
    // "Unable to cast object of type 'System.String[]' to type 'List<string>'" en
    // cualquier alumno con discapacidades o áreas de apoyo asignadas. El JSON que ve
    // el frontend es idéntico en ambos casos (sigue siendo un arreglo).
    public string[] Disabilities { get; set; } = [];
    public string[] AttentionAreas { get; set; } = [];
    public string? CieStatus { get; set; }
    public int? TeaAlertLevel { get; set; }
}

public sealed class CanalizacionMonthCountDto
{
    public string Estado { get; set; } = string.Empty;
    public int Total { get; set; }
}
