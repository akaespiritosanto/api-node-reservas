using api_node_reservas.Dtos;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                             Umbraco processing API
================================================================================
 These endpoints run the conversion from Umbraco source rows to the knowledge
 database using mappings defined in the Umbraco mapping repository.
================================================================================
*/
[ApiController]
[Route("api/umbraco")]
[Produces("application/json")]
public class Processamento_UmbracoController : ControllerBase
{
    private readonly KnowledgeProcessingService processingService;

    public Processamento_UmbracoController(KnowledgeProcessingService processingService)
    {
        this.processingService = processingService;
    }

    [HttpPost("processamento/{mappingId:int}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessingResultDto>> Process(int mappingId, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessUmbracoMappingAsync(mappingId, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(result);
    }

    [HttpPost("processamento/tabela/{tableName}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessingResultDto>> ProcessByTableName(string tableName, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessUmbracoMappingByTableNameAsync(tableName, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(result);
    }
}
