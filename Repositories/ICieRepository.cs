using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ICieRepository
{
    Task<IReadOnlyList<CieDimensionDto>> GetDimensionesCatalogo();
    Task<IEnumerable<CieEvaluacionListItemDto>> GetEvaluaciones(
        int[] allowedSchoolIds, int[] attentionAreaIds, Guid? studentId, int? schoolYearId, int? dimensionId);
    Task<int> CreateEvaluacion(Guid evaluadorId, CreateCieEvaluacionRequest request);
    Task<Guid?> GetEvaluacionAlumnoId(int evaluacionId);
    Task UpsertRespuestas(int evaluacionId, IReadOnlyList<CieRespuestaUpsertItem> items);
    Task UpsertFonoarticulador(int evaluacionId, IReadOnlyList<CieFonoarticuladorUpsertItem> items);
}
