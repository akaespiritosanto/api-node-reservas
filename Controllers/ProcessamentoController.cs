using api_node_reservas.Dtos;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
|                         ProcessamentoController                              |
================================================================================
| Este controller inicia o processamento de um mapeamento.                      |
|                                                                              |
| Ele nao faz a conversao diretamente. Ele chama o KnowledgeProcessingService,  |
| que contem a logica principal.                                                |
================================================================================
*/
[ApiController]
[Route("api/processamento")]
[Produces("application/json")]
public class ProcessamentoController : ControllerBase
{
    private readonly KnowledgeProcessingService processingService;

    public ProcessamentoController(KnowledgeProcessingService processingService)
    {
        this.processingService = processingService;
    }

    /// <summary>
    /// Processa os registos novos ou atualizados para a KB.
    /// </summary>
    [HttpPost("{mappingId:int}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessingResultDto>> Process(int mappingId, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "O limite deve estar entre 1 e 1000." });
        }

        try
        {
            ProcessingResultDto? result = await processingService.ProcessMappingAsync(mappingId, limit);

            if (result is null)
            {
                return NotFound(new ErrorDto { Message = "Mapeamento nao encontrado." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ErrorDto { Message = exception.Message });
        }
    }
}
