using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IClinicalReadRepository
{
    Task<IEnumerable<EvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? cicloId);

    Task<IEnumerable<TeaAlertDto>> GetTeaAlerts(
        int[] allowedSchoolIds, int[] attentionAreaIds, int? cicloId, int? alertLevel);

    Task<IEnumerable<CieSummaryDto>> GetCieSummary(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? cicloId);

    Task<IEnumerable<StudentDataSheetDto>> GetStudentDataSheet(
        int[] allowedSchoolIds, int[] attentionAreaIds, int? schoolId, int? schoolYearId);
}
