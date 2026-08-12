using System.Security.Claims;
using Back_end_RepostesSAE.Models;
using Back_end_RepostesSAE.Models.Dto;
using Back_end_RepostesSAE.Repositories;
using Back_end_RepostesSAE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back_end_RepostesSAE.Controllers;

[ApiController]
[Route("api/clinical/alumnos")]
[Produces("application/json")]
[Authorize(Roles = "ESPECIALISTA")]
public sealed class ClinicalExpedienteController(
    IExpedienteRepository expedienteRepository,
    ICanalizacionRepository canalizacionRepository,
    IClinicalReadRepository clinicalReadRepository,
    ITeaRepository teaRepository,
    ICitaRepository citaRepository,
    ISesionRepository sesionRepository,
    IScopeRepository scopeRepository,
    ExpedientePdfService expedientePdfService,
    IConfiguration configuration) : ControllerBase
{
    // El expediente es "historial educativo completo" para cualquier tipo de especialista
    // (Comunicación/Psicología/Aprendizaje) — a diferencia de otros endpoints de este
    // controlador-hermano (CanalizacionesController, EvaluacionesController) que sí están
    // acotados a la agenda clínica propia de Psicología, aquí se consideran las 4 áreas de
    // apoyo para no dejar fuera de "scope" a alumnos que no tienen casos de psicología.
    private int[] TodasLasAreaIds =>
        configuration.GetSection("TodasLasAreaIds").Get<int[]>() ?? [1, 2, 3, 4];

    [HttpGet("{id:guid}/expediente")]
    public async Task<IActionResult> GetExpediente(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(id, schoolIds, TodasLasAreaIds))
            return Forbid();

        var dto = await BuildExpediente(id, schoolIds);
        if (dto == null) return NotFound("Alumno no encontrado.");

        return Ok(ApiResponse<ExpedienteDto>.Ok(dto));
    }

    /// <summary>
    /// Mismo expediente que GetExpediente, en PDF — para archivar/imprimir/compartir con
    /// otro profesional. Reusa exactamente el mismo control de acceso, no crea un permiso
    /// nuevo.
    /// </summary>
    [HttpGet("{id:guid}/expediente/pdf")]
    public async Task<IActionResult> GetExpedientePdf(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schoolIds = await scopeRepository.GetAllowedSchoolIds(userId.Value);
        if (!await scopeRepository.IsStudentInScope(id, schoolIds, TodasLasAreaIds))
            return Forbid();

        var dto = await BuildExpediente(id, schoolIds);
        if (dto == null) return NotFound("Alumno no encontrado.");

        var pdfBytes = expedientePdfService.Build(dto);
        var nombreArchivo = $"expediente-{dto.Alumno.NombreCompleto.Replace(' ', '-')}.pdf";
        return File(pdfBytes, "application/pdf", nombreArchivo);
    }

    private async Task<ExpedienteDto?> BuildExpediente(Guid id, int[] schoolIds)
    {
        var alumno = await expedienteRepository.GetAlumnoBasico(id);
        if (alumno == null) return null;

        var canalizaciones = await canalizacionRepository.GetByAlumno(id);
        var evaluaciones = await clinicalReadRepository.GetEvaluaciones(schoolIds, TodasLasAreaIds, id, null);
        var teaHistorial = await teaRepository.GetHistory(id);
        var citas = await citaRepository.List(schoolIds, TodasLasAreaIds, null, null, id);
        var sesiones = await sesionRepository.GetByStudent(id);
        var cieResumen = await clinicalReadRepository.GetCieSummary(schoolIds, TodasLasAreaIds, id, null);
        var actividades = await expedienteRepository.GetActividades(id);

        return new ExpedienteDto
        {
            Alumno = alumno,
            Canalizaciones = canalizaciones.ToList(),
            Evaluaciones = evaluaciones.ToList(),
            TeaHistorial = teaHistorial.ToList(),
            Citas = citas.ToList(),
            Sesiones = sesiones.ToList(),
            CieResumen = cieResumen.ToList(),
            Actividades = actividades.ToList()
        };
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
