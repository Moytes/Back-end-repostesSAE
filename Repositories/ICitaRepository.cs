using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ICitaRepository
{
    Task<AgendarCitaResultDto> Create(Guid especialistaId, AgendarCitaRequest request);
    Task<IEnumerable<CitaListItemDto>> List(
        int[] allowedSchoolIds, int[] attentionAreaIds,
        DateOnly? from, DateOnly? to, Guid? alumnoId);
    Task<IEnumerable<TutorCitaListItemDto>> ListByAlumno(Guid alumnoId, DateOnly? from = null, DateOnly? to = null);
    Task<bool> UpdateEstado(int id, string estado);
    Task<Guid?> GetAlumnoId(int id);
}
