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
public sealed class TeaController(ITeaRepository teaRepository) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        var connected = await teaRepository.CanConnect();
        return connected
            ? Ok(ApiResponse<object>.Ok(new { message = "Clinical Intelligence MS is healthy. DB: Connected" }))
            : StatusCode(503, ApiResponse<object>.Ok(new { message = "DB: Disconnected" }, "Service unavailable", "CL_503"));
    }

    [HttpPost("evaluate/tea")]
    public async Task<IActionResult> EvaluateTea([FromBody] TeaEvaluationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = TeaScoringService.CalculateScore(request.Answers);

        var id = await teaRepository.Create(
            request.StudentId, userId.Value, request.CicloId,
            result.TotalScore, result.AlertLevel, request.ContextoObs);

        return Ok(ApiResponse<TeaScoringResultDto>.Ok(result));
    }

    [HttpGet("history/tea")]
    public async Task<IActionResult> GetHistory([FromQuery] Guid studentId)
    {
        var items = await teaRepository.GetHistory(studentId);
        return Ok(ApiResponse<IEnumerable<TeaScreeningDto>>.Ok(items));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
