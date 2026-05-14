using api_node_reservas.Dtos;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

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
    /// Cria as tabelas Node, Context e Arc se ainda nao existirem.
    /// </summary>
    [HttpPost("criar-tabelas-kb")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateKnowledgeTables()
    {
        await processingService.CreateKnowledgeTablesAsync();
        return NoContent();
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
