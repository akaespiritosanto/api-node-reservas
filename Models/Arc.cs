namespace api_node_reservas.Models;

public class Arc
{
    public int Id { get; set; }
    public int NodeId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
