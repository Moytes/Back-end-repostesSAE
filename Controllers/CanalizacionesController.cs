using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/canalizaciones")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA_PSI")]
public sealed class CanalizacionesController(
    IScopeRepository scopeRepository,
    ICanalizacionRepository canalizacionRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] EstadosValidos =
        ["PENDIENTE", "RECIBIDA", "EN_PROCESO", "CERRADA"];

    private int[] PsicologiaAreaIds => configuration.GetSection("PsicologiaAreaIds").Get<int[]>() ?? [2, 3];

    [HttpGet]
    public async Task<IActionResult> GetCanalizaciones(
        [FromQuery] string? estado = null,
        [FromQuery] Guid? solicitanteId = null,
        [FromQuery] Guid? receptorId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await canalizacionRepository.GetCanalizaciones(
            schoolIds, PsicologiaAreaIds, estado, solicitanteId, receptorId);

        return Ok(ApiResponse<IEnumerable<CanalizacionListItemDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddCanalizacionRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Motivo))
            return BadRequest("El motivo de la canalización es obligatorio.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(request.AlumnoId, schoolIds, PsicologiaAreaIds))
            return Forbid();

        var id = await canalizacionRepository.Create(request);
        return StatusCode(201, ApiResponse<int>.Created(id));
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateCanalizacionEstadoRequest request)
    {
        var estado = request.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!EstadosValidos.Contains(estado))
            return BadRequest("Estado inválido.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var alumnoId = await canalizacionRepository.GetAlumnoId(id);
        if (alumnoId == null)
            return NotFound("Canalización no encontrada.");

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(alumnoId.Value, schoolIds, PsicologiaAreaIds))
            return Forbid();

        await canalizacionRepository.UpdateEstado(id, estado);
        return Ok(ApiResponse<object>.Ok(new { id, estado }));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
