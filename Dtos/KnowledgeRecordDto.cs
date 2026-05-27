namespace api_node_reservas.Dtos;

public class KnowledgeRecordDto
{
    public string SourceTable { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string TipoE { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string IdInformacao { get; set; } = string.Empty;
    public string Par1 { get; set; } = string.Empty;
    public string Par2 { get; set; } = string.Empty;
    public string Par3 { get; set; } = string.Empty;
    public string Par4 { get; set; } = string.Empty;
    public string Par5 { get; set; } = string.Empty;
    public string Par6 { get; set; } = string.Empty;
    public string Par7 { get; set; } = string.Empty;
    public List<string> Contexts { get; set; } = new List<string>();
    public List<string> Parent { get; set; } = new List<string>();
    public List<KnowledgeRelationDto> Relations { get; set; } = new List<KnowledgeRelationDto>();
}
