using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Controllers;

/*
================================================================================
|                       ProdutosReservadosController                           |
================================================================================
| Este controller permite consultar produtos reservados da base de origem.      |
|                                                                              |
| Tal como o ReservasController, ele mostra os dados antes do processamento.    |
================================================================================
*/
[ApiController]
[Route("api/produtos-reservados")]
[Produces("application/json")]
public class ProdutosReservadosController : ControllerBase
{
    private readonly ReservasDbContext dbContext;

    public ProdutosReservadosController(ReservasDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Lista produtos reservados da tabela ProdutoReservado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProdutoReservado>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ProdutoReservado>>> GetAll([FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "O limite deve estar entre 1 e 1000." });
        }

        List<ProdutoReservado> produtos = await dbContext.ProdutosReservados
            .AsNoTracking()
            .OrderBy(produto => produto.Id)
            .Take(limit)
            .ToListAsync();

        return Ok(produtos);
    }

    /// <summary>
    /// Obtem um produto reservado pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProdutoReservado), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoReservado>> GetById(int id)
    {
        ProdutoReservado? produto = await dbContext.ProdutosReservados.AsNoTracking().FirstOrDefaultAsync(produto => produto.Id == id);

        if (produto is null)
        {
            return NotFound(new ErrorDto { Message = "Produto reservado nao encontrado." });
        }

        return Ok(produto);
    }
}
