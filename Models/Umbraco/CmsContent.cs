namespace api_node_reservas.Models;

// Minimal POCOs for common Umbraco tables used by the mapping logic.
public class CmsContent
{
    public int pk { get; set; }
    public int nodeId { get; set; }
    public int contentType { get; set; }
}
