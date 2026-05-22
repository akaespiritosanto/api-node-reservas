using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Controllers;

/*
================================================================================
|                            ReservasController                                |
================================================================================
| Este controller permite consultar reservas da base de dados de origem.        |
|                                                                              |
| Ele serve para ver os dados brutos antes de eles serem mapeados para a base   |
| de conhecimento.                                                             |
================================================================================
*/
[ApiController]
[Route("api/reservas")]
[Produces("application/json")]
public class ReservasController : ControllerBase
{
    private readonly ReservasDbContext dbContext;

    public ReservasController(ReservasDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Lista reservas da tabela Reserva.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Reserva>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Reserva>>> GetAll([FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "O limite deve estar entre 1 e 1000." });
        }

        List<Reserva> reservas = await dbContext.Reservas
            .AsNoTracking()
            .OrderBy(reserva => reserva.Id)
            .Take(limit)
            .ToListAsync();

        return Ok(reservas);
    }

    /// <summary>
    /// Obtem uma reserva pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Reserva), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Reserva>> GetById(int id)
    {
        Reserva? reserva = await dbContext.Reservas.AsNoTracking().FirstOrDefaultAsync(reserva => reserva.Id == id);

        if (reserva is null)
        {
            return NotFound(new ErrorDto { Message = "Reserva nao encontrada." });
        }

        return Ok(reserva);
    }
}
