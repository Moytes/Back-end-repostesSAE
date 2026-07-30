namespace Back_end_RepostesSAE.Repositories;

/// <summary>
/// Acceso a la DB compartida (SAEV3) para resolver el alcance del psicólogo:
/// escuelas permitidas (asignadas + zona) y verificación de alumnos en el área de Psicología.
/// </summary>
public interface IScopeRepository
{
    Task<int[]> GetAllowedSchoolIds(Guid userId);
    Task<bool> IsStudentInScope(Guid studentId, int[] allowedSchoolIds, int[] attentionAreaIds);
}
