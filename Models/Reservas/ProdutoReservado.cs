namespace api_node_reservas.Models;

public class ProdutoReservado
{
    public int Id { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int IdReserva { get; set; }
    public int IdProduto { get; set; }
    public string? Referencia { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataActualizacao { get; set; }
}
