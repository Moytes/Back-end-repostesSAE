using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Back_end_RepostesSAE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical")]
[Produces("application/json")]
[Authorize]
public sealed class TeaController(
    ITeaRepository teaRepository,
    IScopeRepository scopeRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] SeguimientoEstados =
        ["ACTIVA", "EN_MONITOREO", "NOTIFICADA", "RESUELTA"];

    // Igual que en los demás controladores clínicos: cualquier tipo de especialista aplica
    // tamizajes TEA, no solo Psicología.
    private int[] PsicologiaAreaIds =>
        configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        var connected = await teaRepository.CanConnect();
        return connected
            ? Ok(ApiResponse<object>.Ok(new { message = "Clinical Intelligence MS is healthy. DB: Connected" }))
            : StatusCode(503, ApiResponse<object>.Ok(new { message = "DB: Disconnected" }, "Service unavailable", "CL_503"));
    }

    [HttpGet("tea/indicadores")]
    [Authorize(Roles = "ESPECIALISTA")]
    public async Task<IActionResult> GetIndicadores()
    {
        var items = await teaRepository.GetIndicadores();
        return Ok(ApiResponse<IReadOnlyList<TeaIndicadorDto>>.Ok(items));
    }

    [HttpPost("evaluate/tea")]
    [Authorize(Roles = "ESPECIALISTA")]
    public async Task<IActionResult> EvaluateTea([FromBody] TeaEvaluationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(request.StudentId, schoolIds, PsicologiaAreaIds))
            return Forbid();

        var respuestas = request.Respuestas;
        if (respuestas is { Count: > 0 })
        {
            foreach (var r in respuestas)
            {
                if (r.Frecuencia is < 0 or > 3 || r.Intensidad is < 0 or > 3)
                    return BadRequest("Frecuencia e intensidad deben estar entre 0 y 3.");
            }

            var id = await teaRepository.CreateWithRespuestas(
                request.StudentId, userId.Value, request.CicloId, request.ContextoObs, respuestas);
            var score = await teaRepository.GetScreeningScore(id);
            var total = score?.PuntajeTotal ?? 0;
            var level = score?.NivelAlerta ?? "SIN_ALERTA";
            return Ok(ApiResponse<object>.Ok(new { id, TotalScore = total, AlertLevel = level }));
        }

        var result = TeaScoringService.CalculateScore(request.Answers);
        var legacyId = await teaRepository.Create(
            request.StudentId, userId.Value, request.CicloId,
            result.TotalScore, result.AlertLevel, request.ContextoObs);

        return Ok(ApiResponse<object>.Ok(new { id = legacyId, result.TotalScore, result.AlertLevel }));
    }

    [HttpGet("history/tea")]
    public async Task<IActionResult> GetHistory([FromQuery] Guid studentId)
    {
        var items = await teaRepository.GetHistory(studentId);
        return Ok(ApiResponse<IEnumerable<TeaScreeningDto>>.Ok(items));
    }

    [HttpPut("tea/alertas/{id:int}/seguimiento")]
    [Authorize(Roles = "ESPECIALISTA")]
    public async Task<IActionResult> UpdateSeguimiento(int id, [FromBody] UpdateTeaSeguimientoRequest request)
    {
        var estado = request.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!SeguimientoEstados.Contains(estado))
            return BadRequest("Estado de seguimiento inválido.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await teaRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Alerta TEA no encontrada.");

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(alumnoId.Value, schoolIds, PsicologiaAreaIds))
            return Forbid();

        await teaRepository.UpdateSeguimiento(id, estado, request.Nota?.Trim());
        return Ok(ApiResponse<object>.Ok(new { id, estado }));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
