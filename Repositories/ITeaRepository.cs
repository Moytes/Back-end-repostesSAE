using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ITeaRepository
{
    Task<int> Create(Guid alumnoId, Guid evaluadorId, int cicloId, int puntajeTotal, string nivelAlerta, string? contextoObs);
    Task<IEnumerable<TeaScreeningDto>> GetHistory(Guid alumnoId);
    Task<bool> CanConnect();
}
