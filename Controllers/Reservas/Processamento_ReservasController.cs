using api_node_reservas.Dtos;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                            Reservas processing API
================================================================================
 These endpoints run the conversion from Reservas source rows to the knowledge
 database. The mapping id chooses which table and fields are processed.
================================================================================
*/
[ApiController]
[Route("api/processamento")]
[Produces("application/json")]
public class Processamento_ReservasController : ControllerBase
{
    private readonly KnowledgeProcessingService processingService;

    // Receives the service that does the real processing work.
    public Processamento_ReservasController(KnowledgeProcessingService processingService)
    {
        this.processingService = processingService;
    }

    /// <summary>
    /// Processes new or updated Reservas records into the knowledge database.
    /// </summary>
    [HttpPost("{mappingId:int}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Processes one mapping by id and returns a summary of what was created or updated.
    public async Task<ActionResult<ProcessingResultDto>> Process(int mappingId, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessMappingAsync(mappingId, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Processes new or updated Reservas records into the knowledge database by table name.
    /// </summary>
    [HttpPost("tabela/{tableName}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Processes one mapping by table name and returns a summary of what was created or updated.
    public async Task<ActionResult<ProcessingResultDto>> ProcessByTableName(string tableName, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessMappingByTableNameAsync(tableName, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(result);
    }
}
