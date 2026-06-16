namespace api_node_reservas.Dtos;

public class KnowledgeParentDto
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public int ParentTypeId { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public string GroupById { get; set; } = string.Empty;
    public string GroupByType { get; set; } = string.Empty;
    public int GroupByTypeId { get; set; }
}
