using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;

/// <summary>
/// Endpoints for creating appointments in icMED.
/// </summary>
[ApiController]
[Route("api/appointments")] // Gateway route
public sealed class AppointmentsController : ControllerBase
{
    private readonly IIcMedClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="AppointmentsController"/>.
    /// </summary>
    /// <param name="client">Abstraction over the icMED API.</param>
    public AppointmentsController(IIcMedClient client) => _client = client;

    /// <summary>
    /// Creates a new appointment in icMED.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] AppointmentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest("FirstName and LastName are required");
        }
        if (request.From == default || request.To == default || request.To <= request.From)
        {
            return BadRequest("Invalid time interval");
        }
        var result = await _client.CreateAppointmentAsync(request, ct);
        return Ok(result);
    }
}
