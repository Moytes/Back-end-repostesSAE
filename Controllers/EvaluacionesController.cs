using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/evaluaciones-psicopedagogicas")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA_PSI")]
public sealed class EvaluacionesController(
    IScopeRepository scopeRepository,
    IClinicalReadRepository clinicalRepository,
    IEvaluacionRepository evaluacionRepository,
    IConfiguration configuration) : ControllerBase
{
    private int PsicologiaAreaId => configuration.GetValue("PsicologiaAreaId", 2);

    [HttpGet]
    public async Task<IActionResult> GetEvaluaciones(
        [FromQuery] Guid? studentId = null,
        [FromQuery] int? schoolYearId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetEvaluaciones(schoolIds, PsicologiaAreaId, studentId, schoolYearId);

        return Ok(ApiResponse<IEnumerable<EvaluacionListItemDto>>.Ok(items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var detail = await evaluacionRepository.GetDetail(id);
        if (detail == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, detail.AlumnoId))
            return Forbid();

        return Ok(ApiResponse<EvaluacionDetailDto>.Ok(detail));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveEvaluacionRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!await IsInScope(userId.Value, request.AlumnoId))
            return Forbid();

        var id = await evaluacionRepository.Create(request);
        return StatusCode(201, ApiResponse<int>.Created(id));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveEvaluacionRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await evaluacionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await evaluacionRepository.Update(id, request);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    private async Task<bool> IsInScope(Guid userId, Guid alumnoId)
    {
        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId);
        return await scopeRepository.IsStudentInScope(alumnoId, schoolIds, PsicologiaAreaId);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
