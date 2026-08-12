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
[Authorize(Roles = "ESPECIALISTA")]
public sealed class EvaluacionesController(
    IScopeRepository scopeRepository,
    IClinicalReadRepository clinicalRepository,
    IEvaluacionRepository evaluacionRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] EstadosEditables = ["BORRADOR", "EN_REVISION"];

    // Igual que en ClinicalExpedienteController/ReportesController: cualquier tipo de
    // especialista puede levantar evaluaciones, no solo Psicología.
    private int[] PsicologiaAreaIds => configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet]
    public async Task<IActionResult> GetEvaluaciones(
        [FromQuery] Guid? studentId = null,
        [FromQuery] int? schoolYearId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetEvaluaciones(schoolIds, PsicologiaAreaIds, studentId, schoolYearId);

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
        request.Estado = request.Estado?.Trim().ToUpperInvariant() ?? "BORRADOR";
        if (request.AlumnoId == Guid.Empty
            || request.CicloId <= 0
            || string.IsNullOrWhiteSpace(request.MotivoEvaluacion)
            || request.AreasEvaluar.Length == 0
            || request.InstrumentosAplicar.Length == 0)
            return BadRequest("Alumno, ciclo escolar, motivo, áreas e instrumentos son obligatorios.");

        if (!EstadosEditables.Contains(request.Estado))
            return BadRequest("Estado inválido para una nueva evaluación.");

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

    [HttpGet("{id:int}/bap")]
    public async Task<IActionResult> GetBap(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await evaluacionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        var items = await evaluacionRepository.GetBap(id);
        return Ok(ApiResponse<IReadOnlyList<EvalPsicoBapDto>>.Ok(items));
    }

    [HttpPut("{id:int}/bap")]
    public async Task<IActionResult> ReplaceBap(int id, [FromBody] ReplaceEvalPsicoBapRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await evaluacionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await evaluacionRepository.ReplaceBap(id, request.Items);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    [HttpGet("{id:int}/colaboradores")]
    public async Task<IActionResult> GetColaboradores(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await evaluacionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        var items = await evaluacionRepository.GetColaboradores(id);
        return Ok(ApiResponse<IReadOnlyList<EvalPsicoColaboradorDto>>.Ok(items));
    }

    [HttpPut("{id:int}/colaboradores")]
    public async Task<IActionResult> ReplaceColaboradores(int id, [FromBody] ReplaceEvalPsicoColaboradoresRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var alumnoId = await evaluacionRepository.GetAlumnoId(id);
        if (alumnoId == null) return NotFound("Evaluación no encontrada.");

        if (!await IsInScope(userId.Value, alumnoId.Value))
            return Forbid();

        await evaluacionRepository.ReplaceColaboradores(id, request.Items);
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
