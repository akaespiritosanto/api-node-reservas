namespace api_node_reservas.Models;

/*
================================================================================
|                                 Context                                      |
================================================================================
| Esta classe representa textos extra ligados a um Node.                        |
|                                                                              |
| No processamento, os campos definidos em Contexts no mapeamento sao gravados  |
| nesta tabela.                                                                 |
================================================================================
*/
public class Context
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Location { get; set; }
    public int NodeId { get; set; }
    public string? Par1 { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DescriptionType { get; set; }
}
