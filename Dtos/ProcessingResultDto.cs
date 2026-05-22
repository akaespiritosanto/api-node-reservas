namespace api_node_reservas.Dtos;

/*
================================================================================
|                           ProcessingResultDto                                |
================================================================================
| Este DTO e a resposta devolvida depois de processar um mapeamento.            |
|                                                                              |
| Ele mostra quantos registos foram processados e quantos Nodes, Contexts e Arcs|
| foram criados ou atualizados.                                                 |
================================================================================
*/
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
