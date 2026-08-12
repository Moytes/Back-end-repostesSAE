using Back_end_RepostesSAE.Models.Dto;

namespace Back_end_RepostesSAE.Repositories;

public interface ITeaRepository
{
    Task<int> Create(Guid alumnoId, Guid evaluadorId, int cicloId, int puntajeTotal, string nivelAlerta, string? contextoObs);
    Task<int> CreateWithRespuestas(
        Guid alumnoId, Guid evaluadorId, int cicloId, string? contextoObs, IReadOnlyList<TeaRespuestaItem> respuestas);
    Task SaveRespuestas(int screeningId, IReadOnlyList<TeaRespuestaItem> respuestas);
    Task<(int PuntajeTotal, string NivelAlerta)?> GetScreeningScore(int screeningId);
    Task<IReadOnlyList<TeaIndicadorDto>> GetIndicadores();
    Task<IEnumerable<TeaScreeningDto>> GetHistory(Guid alumnoId);
    Task<Guid?> GetAlumnoId(int screeningId);
    Task<bool> UpdateSeguimiento(int screeningId, string estado, string? nota);
    Task<bool> CanConnect();
}
