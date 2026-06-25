namespace api_node_reservas.Models;

// Maps the custom aplog table - tracks API usage and AP access
public class ApLog
{
    public int ID { get; set; }
    public DateTime RequestDate { get; set; }
    public string RequestAP_ID { get; set; } = string.Empty;
    public string RequestAP_AP { get; set; } = string.Empty;
    public string RequestAP_T { get; set; } = string.Empty;
    public string RequestAP_URL { get; set; } = string.Empty;
    public string UsedAP_AP { get; set; } = string.Empty;
    public int UsedAP_LocationID { get; set; }
    public int UsedAP_EntityID { get; set; }
    public string UserIP { get; set; } = string.Empty;
    public string UserBrowserName { get; set; } = string.Empty;
    public string UserBrowserVersion { get; set; } = string.Empty;
    public string UserBrowserLanguage { get; set; } = string.Empty;
    public string UserOSPlatform { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
