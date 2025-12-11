using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using IcMed.IntegrationDemo.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IcMed.IntegrationDemo.Tests.WebApi.Controllers;

/// <summary>
/// Pruebas unitarias para <see cref="IcMed.IntegrationDemo.WebApi.Controllers.WorkplacesController"/>,
/// verificando la devolución de 200 OK con la lista de centros.
/// </summary>
public class WorkplacesControllerTests
{
    /// <summary>
    /// Debe retornar 200 OK con los elementos proporcionados por el puerto <see cref="IIcMedClient"/>.
    /// </summary>
    [Fact]
    public async Task Get_ReturnsOk_WithItems()
    {
        var items = new List<Workplace> { new Workplace(1, 12345, "Name") };
        var client = new Mock<IIcMedClient>();
        client.Setup(c => c.GetWorkplacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var controller = new WorkplacesController(client.Object);
        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(items, ok.Value);
    }
}
