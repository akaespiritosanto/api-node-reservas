namespace api_node_reservas.Dtos;

public class ProcessingResultDto
{
    public int MappingId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int NodesCreated { get; set; }
    public int NodesUpdated { get; set; }
    public int ContextsCreated { get; set; }
    public int ArcsCreated { get; set; }
    public int LastProcessedId { get; set; }
    public DateTime ProcessingDate { get; set; }
    public List<KnowledgeRecordDto> Records { get; set; } = [];
}
