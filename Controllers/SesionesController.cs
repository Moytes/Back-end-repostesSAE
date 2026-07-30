using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA_PSI")]
public sealed class SesionesController(
    IScopeRepository scopeRepository,
    ISesionRepository sesionRepository,
    IConfiguration configuration) : ControllerBase
{
    private int[] PsicologiaAreaIds => configuration.GetSection("PsicologiaAreaIds").Get<int[]>() ?? [2, 3];

    [HttpGet("alumnos/{alumnoId:guid}/sesiones")]
    public async Task<IActionResult> GetByStudent(Guid alumnoId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!await IsInScope(userId.Value, alumnoId))
            return Forbid();

        var items = await sesionRepository.GetByStudent(alumnoId);
        return Ok(ApiResponse<IEnumerable<SesionListItemDto>>.Ok(items));
    }

    [HttpPost("alumnos/{alumnoId:guid}/sesiones")]
    public async Task<IActionResult> Create(Guid alumnoId, [FromBody] AddSesionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nota))
            return BadRequest("La nota de la sesión es obligatoria.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!await IsInScope(userId.Value, alumnoId))
            return Forbid();

        var id = await sesionRepository.Create(alumnoId, userId.Value, request);
        return StatusCode(201, ApiResponse<int>.Created(id));
    }

    [HttpPut("sesiones/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSesionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nota))
            return BadRequest("La nota de la sesión es obligatoria.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await sesionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Sesión no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await sesionRepository.Update(id, request);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    [HttpDelete("sesiones/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await sesionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Sesión no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await sesionRepository.Delete(id);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    private async Task<bool> IsInScope(Guid userId, Guid alumnoId)
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
