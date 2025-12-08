using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;

[ApiController]
[Route("api/specialities")]
/// <summary>
/// Endpoints for retrieving medical specialities for a given workplace.
/// </summary>
public sealed class SpecialitiesController : ControllerBase
{
    private readonly IIcMedClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="SpecialitiesController"/>.
    /// </summary>
    /// <param name="client">Abstraction over the icMED API.</param>
    public SpecialitiesController(IIcMedClient client) => _client = client;

    /// <summary>
    /// Gets specialities for a workplace.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Speciality>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] long workplaceId, CancellationToken ct)
    {
        if (workplaceId <= 0) return BadRequest("workplaceId must be positive");
        var items = await _client.GetSpecialitiesAsync(workplaceId, ct);
        return Ok(items);
    }
}
