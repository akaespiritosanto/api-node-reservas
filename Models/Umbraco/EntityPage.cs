namespace api_node_reservas.Models;

// Umbraco document type: EntityPage
// Configures hotel/entity settings and integration credentials
public class EntityPage
{
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    // Entity Reference and Identification
    public string entityReference { get; set; } = string.Empty;
    public int? entityID { get; set; }

    // Credentials for integration
    public string userName { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;

    // Accommodation reservation settings
    public bool allowAccommodationReservation { get; set; }

    // Integration channel and service identifiers
    public int? idCanal { get; set; }
    public int? clientID { get; set; }
    public int? userID { get; set; }
    public int? serviceID { get; set; }
    public int? productID { get; set; }

    // XRS Classifications
    public string? classifications { get; set; }
}
