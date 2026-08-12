using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/citas")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA")]
public sealed class CitasController(
    IScopeRepository scopeRepository,
    ICitaRepository citaRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] ModalidadesValidas = ["PRESENCIAL", "VIRTUAL"];

    // Igual que en ClinicalExpedienteController/ReportesController: esta agenda es para
    // cualquier tipo de especialista, no solo Psicología, así que se consideran las 4 áreas.
    private int[] PsicologiaAreaIds =>
        configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    private static readonly string[] EstadosCitaValidos = ["PROGRAMADA", "REALIZADA", "CANCELADA"];

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? alumnoId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await citaRepository.List(schoolIds, PsicologiaAreaIds, from, to, alumnoId);
        return Ok(ApiResponse<IEnumerable<CitaListItemDto>>.Ok(items));
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateCitaEstadoRequest request)
    {
        var estado = request.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!EstadosCitaValidos.Contains(estado))
            return BadRequest("Estado de cita inválido.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var alumnoId = await citaRepository.GetAlumnoId(id);
        if (alumnoId == null)
            return NotFound("Cita no encontrada.");

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(alumnoId.Value, schoolIds, PsicologiaAreaIds))
            return Forbid();

        await citaRepository.UpdateEstado(id, estado);
        return Ok(ApiResponse<object>.Ok(new { id, estado }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgendarCitaRequest request)
    {
        request.TipoCita = request.TipoCita.Trim().ToUpperInvariant();
        request.Modalidad = request.Modalidad.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(request.TipoCita)
            || request.Fecha == null
            || request.Hora == null)
            return BadRequest("Tipo de cita, fecha y hora son obligatorios.");

        if (!ModalidadesValidas.Contains(request.Modalidad))
            return BadRequest("Modalidad inválida.");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(request.AlumnoId, schoolIds, PsicologiaAreaIds))
            return Forbid();

        try
        {
            var result = await citaRepository.Create(userId.Value, request);
            return StatusCode(201, ApiResponse<AgendarCitaResultDto>.Created(result));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
