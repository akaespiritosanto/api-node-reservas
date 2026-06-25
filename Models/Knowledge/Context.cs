namespace api_node_reservas.Models;

public class Context
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    // Parent column removed from the database. Its values are now stored
    // in the Location field (see Program.cs migration step). Keep this model
    // simple for beginners: Location now holds the parent context id.
    public int Location { get; set; }
    public int NodeId { get; set; }
    public string? Par1 { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DescriptionType { get; set; }
}
