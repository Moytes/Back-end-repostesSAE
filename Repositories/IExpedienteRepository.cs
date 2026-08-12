using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface IExpedienteRepository
{
    Task<ExpedienteAlumnoDto?> GetAlumnoBasico(Guid alumnoId);
    Task<IReadOnlyList<ExpedienteActividadDto>> GetActividades(Guid alumnoId);
}
