namespace api_node_reservas.Models;

// Maps the umbracoNode table - base hierarchy for all Umbraco content
public class UmbracoNode
{
    public int id { get; set; }
    public bool trashed { get; set; }
    public int parentID { get; set; }
    public int? nodeUser { get; set; }
    public int level { get; set; }
    public string path { get; set; } = string.Empty;
    public int sortOrder { get; set; }
    public Guid? uniqueID { get; set; }
    public string? text { get; set; }
    public Guid? nodeObjectType { get; set; }
    public DateTime createDate { get; set; }
}
