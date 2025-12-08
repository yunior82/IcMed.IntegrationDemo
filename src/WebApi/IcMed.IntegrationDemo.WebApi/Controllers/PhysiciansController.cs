using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;


[ApiController]
[Route("api/physicians")]
/// <summary>
/// Endpoints to retrieve physicians for a given workplace and speciality.
/// </summary>
public sealed class PhysiciansController(IIcMedClient client) : ControllerBase
{
    /// <summary>
    /// Gets physicians for a workplace and speciality.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Physician>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] long workplaceId, [FromQuery] long specialityId, CancellationToken ct)
    {
        if (workplaceId <= 0 || specialityId <= 0) return BadRequest("workplaceId and specialityId must be positive");
        var items = await client.GetPhysiciansAsync(workplaceId, specialityId, ct);
        return Ok(items);
    }
}
