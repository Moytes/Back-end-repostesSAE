using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IEvaluacionRepository
{
    Task<EvaluacionDetailDto?> GetDetail(int id);
    Task<IEnumerable<EvaluacionDetailDto>> GetByStudent(Guid studentId);
    Task<Guid?> GetAlumnoId(int id);
    Task<int> Create(SaveEvaluacionRequest request);
    Task<bool> Update(int id, SaveEvaluacionRequest request);
}
