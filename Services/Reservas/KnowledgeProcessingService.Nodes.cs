using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                   Node saving
    ============================================================================
     A Node is the main knowledge record. One source row normally becomes one
     Node, identified by TypeId + Type.
    ============================================================================
    */

    // Finds the existing Node for this source row, or creates a new one.
    private async Task<Node> FindOrCreateNodeAsync(KnowledgeRecordDto record, SaveResult result)
    {
        int typeId = ToInt(record.IdInformacao);
        string type = LimitText(record.Tipo, 30);

        // One Node is identified by TypeId + Type.
        // Example: id 1 as "Reserva" is different from id 1 as "ProdutoReservado".
        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(existingNode =>
            existingNode.TypeId == typeId && existingNode.Type == type);

        if (node is null)
        {
            node = new Node
            {
                Reference = LimitText(record.Reference, 1000),
                TypeId = typeId,
                Type = type
            };

            knowledgeDbContext.Nodes.Add(node);
            result.NodeCreated = true;
        }
        else
        {
            result.NodeUpdated = true;
        }

        return node;
    }

    // Copies the DTO values into the Node model before saving.
    private static void FillNode(Node node, KnowledgeRecordDto record, DateTime updateDate)
    {
        // This method is the only place where DTO fields are copied into a Node.
        node.Reference = LimitText(record.Reference, 1000);
        node.TypeId = ToInt(record.IdInformacao);
        node.Type = LimitText(record.Tipo, 30);
        node.Description = LimitText(record.Descricao, 8000);
        node.Par1 = LimitText(record.Par1, 200);
        node.Par2 = LimitText(record.Par2, 200);
        node.Par3 = LimitText(record.Par3, 200);
        node.Par4 = LimitText(record.Par4, 200);
        node.Par5 = LimitText(record.Par5, 200);
        node.Par6 = LimitText(record.Par6, 200);
        node.Par7 = LimitText(record.Par7, 200);
        node.Link = string.Empty;
        node.Security = 0;
        node.UpdateDate = updateDate;
        node.UpdateUser = 0;
        node.DescriptionType = null;
    }
}
