namespace api_node_reservas.Models;

/*
================================================================================
|                                  Arc                                         |
================================================================================
| Esta classe representa uma ligacao entre dois Nodes.                          |
|                                                                              |
| Source e o Node de origem. Target e o Node de destino. Type explica o tipo da |
| ligacao.                                                                      |
================================================================================
*/
public class Arc
{
    public int Id { get; set; }
    public int Source { get; set; }
    public int Target { get; set; }
    public int TypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime? UpdateDate { get; set; }
}
