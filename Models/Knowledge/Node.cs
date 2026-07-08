namespace api_node_reservas.Models;

public class Node
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Par1 { get; set; }
    public string? Par2 { get; set; }
    public string? Par3 { get; set; }
    public string? Par4 { get; set; }
    public string? Par5 { get; set; }
    public string? Par6 { get; set; }
    public string? Par7 { get; set; }
    public string Link { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public int Security { get; set; }
    public DateTime UpdateDate { get; set; }
    public int UpdateUser { get; set; }
    public string? DescriptionType { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
    public DateTime? ImportedAt { get; set; }
    public string? SyncStatus { get; set; }
}
