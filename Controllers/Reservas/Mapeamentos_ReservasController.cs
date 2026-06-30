using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                        Reservas mapping configuration API
================================================================================
 These endpoints manage the mapping definitions. A mapping is the set of rules
 that explains how one Reservas source table should be copied to the knowledge
 database.
================================================================================
*/
[ApiController]
[Route("api/mapeamentos")]
[Produces("application/json")]
public class Mapeamentos_ReservasController : ControllerBase
{
    private readonly MappingRepository repository;

    // Receives the repository that reads and writes the mapping JSON file.
    public Mapeamentos_ReservasController(MappingRepository repository)
    {
        this.repository = repository;
    }

    /// <summary>
    /// Lists all Reservas mapping configurations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MappingConfiguration>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    // Returns every mapping saved in Data/reservas-mapeamentos.json.
    public ActionResult<List<MappingConfiguration>> GetAll()
    {
        return Ok(repository.GetAll());
    }

    /// <summary>
    /// Gets one Reservas mapping configuration by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    // Returns one mapping by id, or 404 when it does not exist.
    public ActionResult<MappingConfiguration> GetById(int id)
    {
        MappingConfiguration? mapping = repository.GetById(id);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Gets one Reservas mapping configuration by table name.
    /// </summary>
    [HttpGet("tabela/{tableName}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    // Returns one mapping by table name, or 404 when it does not exist.
    public ActionResult<MappingConfiguration> GetByTableName(string tableName)
    {
        MappingConfiguration? mapping = repository.GetByTableName(tableName);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Creates a new Reservas mapping configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    // Creates a new mapping from the request body.
    public ActionResult<MappingConfiguration> Create(MappingConfigurationDto dto)
    {
        MappingConfiguration mapping = repository.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = mapping.Id }, mapping);
    }

    /// <summary>
    /// Updates one Reservas mapping configuration.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    // Updates one mapping using the id from the URL and the new data from the request body.
    public IActionResult Update(int id, MappingConfigurationDto dto)
    {
        bool updated = repository.Update(id, dto);

        if (!updated)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes one Reservas mapping configuration.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    // Deletes one mapping by id.
    public IActionResult Delete(int id)
    {
        bool deleted = repository.Delete(id);

        if (!deleted)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return NoContent();
    }
}
