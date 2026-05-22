namespace api_node_reservas.Dtos;

/*
================================================================================
|                          KnowledgeRelationDto                                |
================================================================================
| Este DTO transporta uma relacao ja lida do mapeamento.                        |
|                                                                              |
| O processamento usa estes dados para criar registos na tabela Arc.            |
================================================================================
*/
public class KnowledgeRelationDto
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}
