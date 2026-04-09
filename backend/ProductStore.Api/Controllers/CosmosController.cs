using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductStore.Api.DTOs;
using ProductStore.Api.Services;

namespace ProductStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CosmosController(ICosmosGtinValidator cosmos, ILogger<CosmosController> logger) : ControllerBase
{
    /// <summary>Consulta GTIN na Bluesoft Cosmos (pré-visualização no formulário de produto).</summary>
    [HttpGet("gtins/{gtin}")]
    [ProducesResponseType(typeof(CosmosGtinProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CosmosGtinProductDto>> GetGtin(string gtin, CancellationToken cancellationToken)
    {
        logger.LogDebug("Consulta preview Cosmos GTIN {Gtin}", gtin);
        var dto = await cosmos.FetchProductAsync(gtin, cancellationToken);
        return Ok(dto);
    }
}
