using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Services;

/// <summary>
/// Motor de puntuación TEA. Los umbrales y el vocabulario de nivel de alerta
/// deben coincidir con la restricción CHECK de tea_screenings.nivel_alerta.
/// </summary>
public static class TeaScoringService
{
    public static TeaScoringResultDto CalculateScore(IEnumerable<int> answers)
    {
        var total = answers.Sum();

        var alertLevel = total switch
        {
            <= 2 => "SIN_ALERTA",
            <= 7 => "LEVE",
            _ => "SIGNIFICATIVO"
        };

        return new TeaScoringResultDto { TotalScore = total, AlertLevel = alertLevel };
    }
}
