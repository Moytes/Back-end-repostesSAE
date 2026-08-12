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
[Authorize(Roles = "ESPECIALISTA,DOCENTE")]
public sealed class CanalizacionesController(
    IScopeRepository scopeRepository,
    ICanalizacionRepository canalizacionRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] EstadosValidos =
        ["PENDIENTE", "RECIBIDA", "EN_PROCESO", "CERRADA"];
    private static readonly string[] PrioridadesValidas =
        ["BAJA", "MEDIA", "ALTA"];

    // Igual que en ClinicalExpedienteController/ReportesController: cualquier tipo de
    // especialista recibe y atiende canalizaciones, no solo Psicología.
    private int[] PsicologiaAreaIds => configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet]
    public async Task<IActionResult> GetCanalizaciones(
        [FromQuery] string? estado = null,
        [FromQuery] Guid? solicitanteId = null,
        [FromQuery] Guid? receptorId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (User.IsInRole("DOCENTE") && !User.IsInRole("ESPECIALISTA"))
        {
            var own = await canalizacionRepository.GetBySolicitante(userId.Value, estado);
            return Ok(ApiResponse<IEnumerable<CanalizacionListItemDto>>.Ok(own));
        }

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var specialistItems = await canalizacionRepository.GetCanalizaciones(
            schoolIds, PsicologiaAreaIds, estado, solicitanteId, receptorId);

        return Ok(ApiResponse<IEnumerable<CanalizacionListItemDto>>.Ok(specialistItems));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddCanalizacionRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Motivo))
            return BadRequest("El motivo de la canalización es obligatorio.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var isDocente = User.IsInRole("DOCENTE") && !User.IsInRole("ESPECIALISTA");
        if (isDocente)
        {
            if (!await scopeRepository.IsStudentInDocenteGroup(userId.Value, request.AlumnoId))
                return Forbid();
            request.SolicitanteId = userId.Value;
        }
        else
        {
            var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
            if (!await scopeRepository.IsStudentInScope(request.AlumnoId, schoolIds, PsicologiaAreaIds))
                return Forbid();
            request.SolicitanteId ??= userId.Value;
        }

        var id = await canalizacionRepository.Create(request);
        return StatusCode(201, ApiResponse<int>.Created(id));
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "ESPECIALISTA")]
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

    [HttpPut("{id:int}/atender")]
    [Authorize(Roles = "ESPECIALISTA")]
    public async Task<IActionResult> Atender(int id, [FromBody] AtenderCanalizacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TipoAtencion)
            || request.FechaAtencion == null
            || request.DerivarAreaId == null)
            return BadRequest("Tipo de atención, fecha de atención y especialidad derivada son obligatorios.");

        request.TipoAtencion = request.TipoAtencion.Trim();
        request.Prioridad = request.Prioridad.Trim().ToUpperInvariant();
        if (!PrioridadesValidas.Contains(request.Prioridad))
            return BadRequest("Prioridad inválida.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var alumnoId = await canalizacionRepository.GetAlumnoId(id);
        if (alumnoId == null)
            return NotFound("Canalización no encontrada.");

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(alumnoId.Value, schoolIds, PsicologiaAreaIds))
            return Forbid();

        var updated = await canalizacionRepository.Atender(id, userId.Value, request);
        if (!updated)
            return Conflict("La canalización ya fue atendida o cerrada.");

        return Ok(ApiResponse<object>.Ok(new { id, estado = "EN_PROCESO" }));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
