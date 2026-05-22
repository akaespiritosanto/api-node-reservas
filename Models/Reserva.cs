namespace api_node_reservas.Models;

/*
================================================================================
|                                 Reserva                                      |
================================================================================
| Esta classe representa uma linha da tabela Reserva da base de origem.         |
|                                                                              |
| Ela e usada para consultar reservas e tambem como referencia para os campos   |
| que podem ser usados nos mapeamentos.                                         |
================================================================================
*/
public class Reserva
{
    public int Id { get; set; }
    public string? Numero { get; set; }
    public string? Referencia { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? DataPedido { get; set; }
    public DateTime DataActualizacao { get; set; }
    public string NomeUtilizadorConfirmacao { get; set; } = string.Empty;
}
