using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IEvaluacionRepository
{
    Task<EvaluacionDetailDto?> GetDetail(int id);
    Task<IEnumerable<EvaluacionDetailDto>> GetByStudent(Guid studentId);
    Task<IEnumerable<TutorEvaluacionResumenDto>> GetResumenByStudent(Guid studentId);
    Task<Guid?> GetAlumnoId(int id);
    Task<int> Create(SaveEvaluacionRequest request);
    Task<bool> Update(int id, SaveEvaluacionRequest request);
    Task<IReadOnlyList<EvalPsicoBapDto>> GetBap(int evalPsicoId);
    Task ReplaceBap(int evalPsicoId, IReadOnlyList<EvalPsicoBapDto> items);
    Task<IReadOnlyList<EvalPsicoColaboradorDto>> GetColaboradores(int evalPsicoId);
    Task ReplaceColaboradores(int evalPsicoId, IReadOnlyList<EvalPsicoColaboradorDto> items);
}
