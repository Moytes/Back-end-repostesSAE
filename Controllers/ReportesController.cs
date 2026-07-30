using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/reportes")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA_PSI")]
public sealed class ReportesController(
    IScopeRepository scopeRepository,
    IClinicalReadRepository clinicalRepository,
    IConfiguration configuration) : ControllerBase
{
    private int[] PsicologiaAreaIds => configuration.GetSection("PsicologiaAreaIds").Get<int[]>() ?? [2, 3];

    [HttpGet("alertas-tea")]
    public async Task<IActionResult> GetAlertasTea(
        [FromQuery] int? schoolYearId = null,
        [FromQuery] int? alertLevel = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetTeaAlerts(schoolIds, PsicologiaAreaIds, schoolYearId, alertLevel);

        return Ok(ApiResponse<IEnumerable<TeaAlertDto>>.Ok(items));
    }

    [HttpGet("resumen-cie")]
    public async Task<IActionResult> GetResumenCie(
        [FromQuery] Guid? studentId = null,
        [FromQuery] int? schoolYearId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetCieSummary(schoolIds, PsicologiaAreaIds, studentId, schoolYearId);

        return Ok(ApiResponse<IEnumerable<CieSummaryDto>>.Ok(items));
    }

    [HttpGet("sabana-datos")]
    public async Task<IActionResult> GetStudentDataSheet(
        [FromQuery] int? schoolId = null,
        [FromQuery] int? schoolYearId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetStudentDataSheet(schoolIds, PsicologiaAreaIds, schoolId, schoolYearId);

        return Ok(ApiResponse<IEnumerable<StudentDataSheetDto>>.Ok(items));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
