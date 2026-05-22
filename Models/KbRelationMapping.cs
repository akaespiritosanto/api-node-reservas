namespace api_node_reservas.Models;

/*
================================================================================
|                          KbRelationMapping                                   |
================================================================================
| Esta classe configura uma relacao entre Nodes.                                |
|                                                                              |
| Type e o nome da relacao. TargetId indica de que coluna vem o destino dessa   |
| relacao.                                                                      |
================================================================================
*/
public class KbRelationMapping
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}
