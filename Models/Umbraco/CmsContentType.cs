namespace api_node_reservas.Models;

public class CmsContentType
{
    public int pk { get; set; }
    public int nodeId { get; set; }
    public string? alias { get; set; }
    public string? icon { get; set; }
    public string thumbnail { get; set; } = string.Empty;
    public string? description { get; set; }
    public bool isContainer { get; set; }
    public bool allowAtRoot { get; set; }
}
