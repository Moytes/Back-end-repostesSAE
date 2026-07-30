using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

/// <summary>
/// Lectura para el rol TUTOR: a diferencia de EvaluacionesController/SesionesController
/// (que usan alcance por escuela/área del especialista), aquí el alcance es directo —
/// solo los alumnos vinculados a la cuenta, vía los claims "student_id" del JWT.
/// </summary>
[ApiController]
[Route("api/clinical/tutor")]
[Produces("application/json")]
[Authorize(Roles = "TUTOR")]
public sealed class TutorController(
    IEvaluacionRepository evaluacionRepository,
    ISesionRepository sesionRepository) : ControllerBase
{
    [HttpGet("alumnos/{id:guid}/evaluaciones")]
    public async Task<IActionResult> GetEvaluaciones(Guid id)
    {
        if (!GetAllowedStudentIds().Contains(id))
            return Forbid();

        var items = await evaluacionRepository.GetByStudent(id);
        return Ok(ApiResponse<IEnumerable<EvaluacionDetailDto>>.Ok(items));
    }

    [HttpGet("alumnos/{id:guid}/sesiones")]
    public async Task<IActionResult> GetSesiones(Guid id)
    {
        if (!GetAllowedStudentIds().Contains(id))
            return Forbid();

        var items = await sesionRepository.GetByStudent(id);
        return Ok(ApiResponse<IEnumerable<SesionListItemDto>>.Ok(items));
    }

    private List<Guid> GetAllowedStudentIds()
    {
        return User.FindAll("student_id")
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
    }
}
