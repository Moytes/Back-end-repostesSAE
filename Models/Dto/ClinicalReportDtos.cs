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
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? SchoolName { get; set; }
    public int AlertLevel { get; set; }
    public DateOnly ScreeningDate { get; set; }
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
