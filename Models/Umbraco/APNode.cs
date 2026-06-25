namespace api_node_reservas.Models;

// Umbraco document type: APNode
// Stores Access Point configurations for API routing and access control
public class APNode
{
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    // Access Point Properties
    public string idAP { get; set; } = string.Empty;
    public string apType { get; set; } = string.Empty;
    public int? associatedPageNodeId { get; set; }
    public string? ipRange { get; set; }
    public int? locationID { get; set; }
    public int? entityID { get; set; }
    public string successUrl { get; set; } = string.Empty;

    // Rich text content in multiple languages
    public string? textPT { get; set; }
    public string? textEN { get; set; }

    // Configuration flags
    public bool allowReservations { get; set; }
}
