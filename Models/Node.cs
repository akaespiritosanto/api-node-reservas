namespace api_node_reservas.Models;

/*
================================================================================
|                                  Node                                        |
================================================================================
| Esta classe representa a tabela Node da base de conhecimento.                 |
|                                                                              |
| Um Node e a informacao principal criada a partir de uma linha da base de      |
| reservas. Neste projeto, ele e identificado por TypeId + Type.                |
================================================================================
*/
public class Node
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Par1 { get; set; }
    public string? Par2 { get; set; }
    public string? Par3 { get; set; }
    public string? Par4 { get; set; }
    public string? Par5 { get; set; }
    public string? Par6 { get; set; }
    public string? Par7 { get; set; }
    public string Link { get; set; } = string.Empty;
    public int Security { get; set; }
    public DateTime UpdateDate { get; set; }
    public int UpdateUser { get; set; }
    public string? DescriptionType { get; set; }
}
