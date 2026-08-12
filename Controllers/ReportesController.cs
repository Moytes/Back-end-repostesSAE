using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Back_end_RepostesSAE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/reportes")]
[Authorize(Roles = "ESPECIALISTA")]
public sealed class ReportesController(
    IScopeRepository scopeRepository,
    IClinicalReadRepository clinicalRepository,
    SpecialistReportPdfService pdfService,
    IConfiguration configuration) : ControllerBase
{
    // Igual que en ClinicalExpedienteController: esta pantalla es para cualquier tipo de
    // especialista, no solo Psicología, así que se consideran las 4 áreas de apoyo.
    private int[] PsicologiaAreaIds => configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet("alertas-tea")]
    public async Task<IActionResult> GetAlertasTea(
        [FromQuery] int? schoolYearId = null,
        [FromQuery] int? schoolId = null,
        [FromQuery] int? alertLevel = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetTeaAlerts(schoolIds, PsicologiaAreaIds, schoolYearId, schoolId, alertLevel);

        return Ok(ApiResponse<IEnumerable<TeaAlertDto>>.Ok(items));
    }

    [HttpGet("resumen-cie")]
    public async Task<IActionResult> GetResumenCie(
        [FromQuery] Guid? studentId = null,
        [FromQuery] int? schoolYearId = null,
        [FromQuery] int? schoolId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = await clinicalRepository.GetCieSummary(schoolIds, PsicologiaAreaIds, studentId, schoolYearId, schoolId);

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

    [HttpGet("alertas-tea/pdf")]
    public async Task<IActionResult> GetAlertasTeaPdf(
        [FromQuery] int? schoolYearId = null,
        [FromQuery] int? schoolId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var items = (await clinicalRepository.GetTeaAlerts(
            schoolIds, PsicologiaAreaIds, schoolYearId, schoolId, null)).ToList();
        var pdf = pdfService.BuildTeaAlertsPdf(items, schoolYearId, schoolId);
        return File(pdf, "application/pdf", "alertas-tea.pdf");
    }

    [HttpGet("pack-mensual")]
    public async Task<IActionResult> GetPackMensual(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int? schoolYearId = null)
    {
        if (year < 2000 || month is < 1 or > 12)
            return BadRequest("Año o mes inválido.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        var teaAlerts = (await clinicalRepository.GetTeaAlerts(
            schoolIds, PsicologiaAreaIds, schoolYearId, null, null)).ToList();
        var cieSummary = (await clinicalRepository.GetCieSummary(
            schoolIds, PsicologiaAreaIds, null, schoolYearId)).ToList();
        var canalCounts = (await clinicalRepository.GetCanalizacionCountsForMonth(
            schoolIds, PsicologiaAreaIds, year, month, schoolYearId)).ToList();

        var pdf = pdfService.BuildMonthlyPackPdf(year, month, teaAlerts, cieSummary, canalCounts);
        return File(pdf, "application/pdf", $"pack-mensual-{year}-{month:D2}.pdf");
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
