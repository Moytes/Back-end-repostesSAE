using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IClinicalReadRepository
{
    Task<IEnumerable<EvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int attentionAreaId, Guid? studentId, int? cicloId);

    Task<IEnumerable<TeaAlertDto>> GetTeaAlerts(
        int[] allowedSchoolIds, int attentionAreaId, int? cicloId, int? alertLevel);

    Task<IEnumerable<CieSummaryDto>> GetCieSummary(
        int[] allowedSchoolIds, int attentionAreaId, Guid? studentId, int? cicloId);
}
