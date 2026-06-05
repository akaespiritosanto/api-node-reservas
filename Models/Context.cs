namespace api_node_reservas.Models;

public class Context
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Parent { get; set; }
    public int Location { get; set; }
    public int NodeId { get; set; }
    public string? Par1 { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DescriptionType { get; set; }
}
