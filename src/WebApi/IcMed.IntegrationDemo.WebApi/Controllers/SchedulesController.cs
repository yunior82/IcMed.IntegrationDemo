using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;

/// <summary>
/// Endpoints for retrieving schedule intervals for a physician.
/// </summary>
[ApiController]
[Route("api/physicians/{physicianId:long}/schedule")]
public sealed class SchedulesController : ControllerBase
{
    private readonly IIcMedClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="SchedulesController"/>.
    /// </summary>
    /// <param name="client">Abstraction over the icMED API.</param>
    public SchedulesController(IIcMedClient client) => _client = client;

    /// <summary>
    /// Gets schedule intervals for a physician.
    /// </summary>
    [HttpGet("{referenceDate:long}")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(long physicianId, long referenceDate, [FromQuery] long subOfficeId, [FromQuery] string currentView = "week", CancellationToken ct = default)
    {
        if (physicianId <= 0 || subOfficeId <= 0) return BadRequest("physicianId and subOfficeId must be positive");
        if (currentView != "day" && currentView != "week") return BadRequest("currentView must be 'day' or 'week'");
        var schedule = await _client.GetScheduleAsync(physicianId, subOfficeId, referenceDate, currentView, ct);
        return Ok(schedule);
    }
}
