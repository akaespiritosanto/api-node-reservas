namespace api_node_reservas.Models;

public class MappingConfiguration
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string DetectionMethod { get; set; } = "Id";
    public string IdFieldName { get; set; } = "id";
    public string CreationDateFieldName { get; set; } = string.Empty;
    public string UpdateDateFieldName { get; set; } = string.Empty;
    public int LastProcessedId { get; set; }
    public DateTime? LastSuccessfulProcessingDate { get; set; }
    public KbMapping Mapping { get; set; } = new();
}
