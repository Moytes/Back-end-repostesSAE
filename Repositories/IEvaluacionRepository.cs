using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IEvaluacionRepository
{
    Task<EvaluacionDetailDto?> GetDetail(int id);
    Task<Guid?> GetAlumnoId(int id);
    Task<int> Create(SaveEvaluacionRequest request);
    Task<bool> Update(int id, SaveEvaluacionRequest request);
}
