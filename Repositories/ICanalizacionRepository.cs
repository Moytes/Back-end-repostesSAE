using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ICanalizacionRepository
{
    Task<IEnumerable<CanalizacionListItemDto>> GetCanalizaciones(
        int[] allowedSchoolIds, int[] attentionAreaIds,
        string? estado, Guid? solicitanteId, Guid? receptorId);

    Task<int> Create(AddCanalizacionRequest request);

    Task<bool> UpdateEstado(int id, string estado);

    Task<bool> Atender(int id, Guid receptorId, AtenderCanalizacionRequest request);

    Task<Guid?> GetAlumnoId(int id);

    Task<IEnumerable<CanalizacionListItemDto>> GetBySolicitante(Guid solicitanteId, string? estado);

    Task<IEnumerable<CanalizacionListItemDto>> GetByAlumno(Guid alumnoId);
}
