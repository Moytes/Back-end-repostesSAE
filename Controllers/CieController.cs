using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/cie")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA")]
public sealed class CieController(
    ICieRepository cieRepository,
    IScopeRepository scopeRepository,
    IConfiguration configuration) : ControllerBase
{
    // Igual que en los demás controladores clínicos: cualquier tipo de especialista aplica
    // CIE a sus alumnos, no solo Psicología.
    private int[] PsicologiaAreaIds =>
        configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet("catalogos/dimensiones")]
    public async Task<IActionResult> GetDimensiones()
    {
        var items = await cieRepository.GetDimensionesCatalogo();
        return Ok(ApiResponse<IReadOnlyList<CieDimensionDto>>.Ok(items));
    }

    [HttpGet("evaluaciones")]
    public async Task<IActionResult> GetEvaluaciones(
        [FromQuery] Guid? studentId = null,
        [FromQuery] int? schoolYearId = null,
        [FromQuery] int? dimensionId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await cieRepository.GetEvaluaciones(
            schoolIds, PsicologiaAreaIds, studentId, schoolYearId, dimensionId);

        return Ok(ApiResponse<IEnumerable<CieEvaluacionListItemDto>>.Ok(items));
    }

    [HttpPost("evaluaciones")]
    public async Task<IActionResult> CreateEvaluacion([FromBody] CreateCieEvaluacionRequest request)
    {
        if (request.AlumnoId == Guid.Empty || request.CicloId <= 0 || request.DimensionId <= 0)
            return BadRequest("Alumno, ciclo escolar y dimensión son obligatorios.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!await IsStudentInScope(userId.Value, request.AlumnoId))
            return Forbid();

        var id = await cieRepository.CreateEvaluacion(userId.Value, request);
        return StatusCode(201, ApiResponse<int>.Created(id));
    }

    [HttpPost("evaluaciones/{id:int}/respuestas")]
    public async Task<IActionResult> UpsertRespuestas(int id, [FromBody] UpsertCieRespuestasRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await cieRepository.GetEvaluacionAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación CIE no encontrada.");

        if (!await IsStudentInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await cieRepository.UpsertRespuestas(id, request.Items);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    [HttpPost("evaluaciones/{id:int}/fonoarticulador")]
    public async Task<IActionResult> UpsertFonoarticulador(int id, [FromBody] UpsertCieFonoarticuladorRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await cieRepository.GetEvaluacionAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación CIE no encontrada.");

        if (!await IsStudentInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await cieRepository.UpsertFonoarticulador(id, request.Items);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    private async Task<bool> IsStudentInScope(Guid userId, Guid alumnoId)
    {
        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId);
        return await scopeRepository.IsStudentInScope(alumnoId, schoolIds, PsicologiaAreaIds);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
