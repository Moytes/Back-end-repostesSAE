using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ICanalizacionRepository
{
    Task<IEnumerable<CanalizacionListItemDto>> GetCanalizaciones(
        int[] allowedSchoolIds, int attentionAreaId,
        string? estado, Guid? solicitanteId, Guid? receptorId);

    Task<int> Create(AddCanalizacionRequest request);

    Task<bool> UpdateEstado(int id, string estado);

    Task<Guid?> GetAlumnoId(int id);
}
