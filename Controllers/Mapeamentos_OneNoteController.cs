using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                         OneNote mapping configuration API
================================================================================
 These endpoints show the OneNote mapping definitions. OneNote mappings are kept
 in Data/onenote-mapeamentos.json so they do not mix with Reservas mappings.
================================================================================
*/
[ApiController]
[Route("api/onenote/mapeamentos")]
[Produces("application/json")]
public class Mapeamentos_OneNoteController : ControllerBase
{
    private readonly OneNoteMappingRepository mappingRepository;

    // Receives the repository that reads Data/onenote-mapeamentos.json.
    public Mapeamentos_OneNoteController(OneNoteMappingRepository mappingRepository)
    {
        this.mappingRepository = mappingRepository;
    }

    /// <summary>
    /// Lists all OneNote mapping configurations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MappingConfiguration>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Returns every OneNote mapping saved in Data/onenote-mapeamentos.json.
    public ActionResult<List<MappingConfiguration>> GetAll()
    {
        return Ok(mappingRepository.GetAll());
    }

    /// <summary>
    /// Gets one OneNote mapping configuration by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Returns one OneNote mapping by id, or 404 when it does not exist.
    public ActionResult<MappingConfiguration> GetById(int id)
    {
        MappingConfiguration? mapping = mappingRepository.GetById(id);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Gets one OneNote mapping configuration by table name.
    /// </summary>
    [HttpGet("tabela/{tableName}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Returns one OneNote mapping by table name, or 404 when it does not exist.
    public ActionResult<MappingConfiguration> GetByTableName(string tableName)
    {
        MappingConfiguration? mapping = mappingRepository.GetByTableName(tableName);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return Ok(mapping);
    }
}
