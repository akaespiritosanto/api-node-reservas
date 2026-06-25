namespace api_node_reservas.Models;

// Umbraco document type: PaymentTypeNode
// Payment gateway credentials and parameters
public class PaymentTypeNode
{
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    // Payment Type Identification
    public string nome { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string? descricaoCompleta { get; set; }

    // Payment Gateway URLs
    public string? urlSucesso { get; set; }
    public string? urlErro { get; set; }

    // XRS Payment Integration
    public int? xrsPaymentID { get; set; }

    // Payment Configuration
    public int? visibility { get; set; }
    public int? paymentAntecedenceDays { get; set; }
    public string? additionalCostPercentage { get; set; }
    public string? additionalCostAbsolute { get; set; }
}
