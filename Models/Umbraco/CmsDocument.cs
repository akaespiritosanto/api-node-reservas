namespace api_node_reservas.Models;

public class CmsDocument
{
    public int nodeId { get; set; }
    public bool published { get; set; }
    public int documentUser { get; set; }
    public Guid versionId { get; set; }
    public string text { get; set; } = string.Empty;
    public DateTime updateDate { get; set; }
}
