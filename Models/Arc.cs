namespace api_node_reservas.Models;

public class Arc
{
    public int Id { get; set; }
    public int Source { get; set; }
    public int Target { get; set; }
    public int TypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime? UpdateDate { get; set; }
}
