using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;

[ApiController]
[Route("api/workplaces")]
/// <summary>
/// Exposes endpoints to retrieve workplaces from icMED.
/// </summary>
public sealed class WorkplacesController : ControllerBase
{
    private readonly IIcMedClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="WorkplacesController"/>.
    /// </summary>
    /// <param name="client">Abstraction over the icMED API.</param>
    public WorkplacesController(IIcMedClient client) => _client = client;

    /// <summary>
    /// Gets workplaces from icMED.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Workplace>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var items = await _client.GetWorkplacesAsync(ct);
        return Ok(items);
    }
}
