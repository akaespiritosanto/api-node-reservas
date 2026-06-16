namespace api_node_reservas.Models;

public class KbMapping
{
    public string Tabela { get; set; } = string.Empty;
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
    public string Link { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Security { get; set; } = string.Empty;
    public string UpdateUser { get; set; } = string.Empty;
    public string DescriptionType { get; set; } = string.Empty;
    public string ContextPar1 { get; set; } = string.Empty;
    public string ContextDescriptionType { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public List<string> Contexts { get; set; } = new List<string>();
    public List<KbParentMapping> Parent { get; set; } = new List<KbParentMapping>();
    public List<KbRelationMapping> Relations { get; set; } = new List<KbRelationMapping>();
}
