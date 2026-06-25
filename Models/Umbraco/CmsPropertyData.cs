namespace api_node_reservas.Models;

// Maps the cmsPropertyData table - stores actual property values for content nodes
// Values can be stored in different columns depending on data type
public class CmsPropertyData
{
    public int id { get; set; }
    public int contentNodeId { get; set; }
    public Guid? versionId { get; set; }
    public int propertytypeid { get; set; }
    public int? dataInt { get; set; }
    public DateTime? dataDate { get; set; }
    public string? dataNvarchar { get; set; }
    public string? dataNtext { get; set; }
}
