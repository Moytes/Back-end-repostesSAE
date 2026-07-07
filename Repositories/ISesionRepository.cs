using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ISesionRepository
{
    Task<IEnumerable<SesionListItemDto>> GetByStudent(Guid alumnoId);
    Task<int> Create(Guid alumnoId, Guid psicologoId, AddSesionRequest request);
    Task<bool> Update(int id, UpdateSesionRequest request);
    Task<bool> Delete(int id);
    Task<Guid?> GetAlumnoId(int id);
}
