namespace api_node_reservas.Models;

public class Context
{
    public int Id { get; set; }
    public int NodeId { get; set; }
    public string Valor { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
