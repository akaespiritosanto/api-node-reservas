using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

[ApiController]
[Route("api/mapeamentos/umbraco")]
[Produces("application/json")]
public class Mapeamentos_UmbracoController : ControllerBase
{
    private readonly UmbracoMappingRepository repository;

    public Mapeamentos_UmbracoController(UmbracoMappingRepository repository)
    {
        this.repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MappingConfiguration>), StatusCodes.Status200OK)]
    public ActionResult<List<MappingConfiguration>> GetAll()
    {
        return Ok(repository.GetAll());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public ActionResult<MappingConfiguration> GetById(int id)
    {
        MappingConfiguration? mapping = repository.GetById(id);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(mapping);
    }

    [HttpGet("tabela/{tableName}")]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public ActionResult<MappingConfiguration> GetByTableName(string tableName)
    {
        MappingConfiguration? mapping = repository.GetByTableName(tableName);

        if (mapping is null)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return Ok(mapping);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MappingConfiguration), StatusCodes.Status201Created)]
    public ActionResult<MappingConfiguration> Create(MappingConfigurationDto dto)
    {
        MappingConfiguration mapping = repository.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = mapping.Id }, mapping);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, MappingConfigurationDto dto)
    {
        bool updated = repository.Update(id, dto);

        if (!updated)
        {
            return NotFound(new ErrorDto { Message = "Mapping not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
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
