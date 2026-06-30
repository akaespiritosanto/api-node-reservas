using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                         OneNote mapping configuration API
================================================================================
 These endpoints manage the OneNote mapping definitions. OneNote mappings are
 kept in Data/onenote-mapeamentos.json so they do not mix with Reservas mappings.
================================================================================
*/
[ApiController]
[Route("api/onenote/mapeamentos")]
[Produces("application/json")]
public class Mapeamentos_OneNoteController : ControllerBase
{
    private readonly OneNoteMappingRepository mappingRepository;

    // Receives the repository that reads and writes Data/onenote-mapeamentos.json.
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

    /// <summary>
    /// Creates a new OneNote mapping configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Creates a new OneNote mapping from the request body.
    public ActionResult<MappingConfiguration> Create(MappingConfigurationDto dto)
    {
        MappingConfiguration mapping = mappingRepository.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = mapping.Id }, mapping);
    }

    /// <summary>
    /// Updates one OneNote mapping configuration.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Updates one OneNote mapping using the id from the URL and the new data from the request body.
    public IActionResult Update(int id, MappingConfigurationDto dto)
    {
        bool updated = mappingRepository.Update(id, dto);

        if (!updated)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes one OneNote mapping configuration.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Deletes one OneNote mapping by id.
    public IActionResult Delete(int id)
    {
        bool deleted = mappingRepository.Delete(id);

        if (!deleted)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return NoContent();
    }
}
