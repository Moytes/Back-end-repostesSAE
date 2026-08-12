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
    ICitaRepository citaRepository) : ControllerBase
{
    [HttpGet("alumnos/{id:guid}/evaluaciones")]
    public async Task<IActionResult> GetEvaluaciones(Guid id)
    {
        if (!GetAllowedStudentIds().Contains(id))
            return Forbid();

        var items = await evaluacionRepository.GetResumenByStudent(id);
        return Ok(ApiResponse<IEnumerable<TutorEvaluacionResumenDto>>.Ok(items));
    }

    [HttpGet("alumnos/{id:guid}/citas")]
    public async Task<IActionResult> GetCitas(
        Guid id,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        if (!GetAllowedStudentIds().Contains(id))
            return Forbid();

        var items = await citaRepository.ListByAlumno(id, from, to);
        return Ok(ApiResponse<IEnumerable<TutorCitaListItemDto>>.Ok(items));
    }

    private List<Guid> GetAllowedStudentIds()
    {
        return User.FindAll("student_id")
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
    }
}
