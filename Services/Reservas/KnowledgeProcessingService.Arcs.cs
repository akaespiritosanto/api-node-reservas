using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                    Arc saving
    ============================================================================
     Arc rows are relations between two Nodes. For example, a ProdutoReservado
     Node can point to the Reserva Node it belongs to.
    ============================================================================
    */

    // Creates all Arc relation rows for one Node.
    private async Task AddArcsAsync(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
        // Parent values are saved as "parent" relations.
        foreach (string parent in record.Parent)
        {
            await AddArcAsync(node, string.Empty, "parent", parent, record.ParentType, updateDate, result);
        }

        // Custom relation mappings are saved with the relation type from the mapping file.
        foreach (KnowledgeRelationDto relation in record.Relations)
        {
            await AddArcAsync(node, relation.TypeId, relation.Type, relation.TargetId, relation.TargetType, updateDate, result);
        }
    }

    // Creates one Arc row if the target Node can be found safely.
    private async Task AddArcAsync(
        Node node,
        string relationTypeIdText,
        string relationType,
        string targetIdText,
        string targetType,
        DateTime updateDate,
        SaveResult result)
    {
        if (string.IsNullOrWhiteSpace(targetIdText))
        {
            // Without a target id there is no relation to create.
            return;
        }

        int? targetNodeId = await FindTargetNodeIdAsync(targetIdText, targetType);

        if (targetNodeId is null)
        {
            // The source Node is still saved, but the relation is skipped.
            logger.LogWarning(
                "Arc target was not found or is ambiguous. Source node: {SourceNodeId}, TargetId: {TargetId}, TargetType: {TargetType}.",
                node.Id,
                targetIdText,
                targetType);

            return;
        }

        knowledgeDbContext.Arcs.Add(new Arc
        {
            Source = node.Id,
            Target = targetNodeId.Value,
            TypeId = ToInt(relationTypeIdText),
            Type = LimitText(relationType, 50),
            UpdateDate = updateDate
        });

        result.ArcsCreated++;
    }

    // Finds the database id of the target Node used by an Arc relation.
    private async Task<int?> FindTargetNodeIdAsync(string targetIdText, string targetType)
    {
        int targetTypeId = ToInt(targetIdText);

        // If the relation tells us the target type, the lookup is exact.
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            Node? targetNode = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(node =>
                node.TypeId == targetTypeId && node.Type == targetType);

            return targetNode?.Id;
        }

        // Without a type, the id must match exactly one Node.
        // If it matches zero or many Nodes, the relation would be unsafe.
        List<Node> possibleTargets = await knowledgeDbContext.Nodes
            .Where(node => node.TypeId == targetTypeId)
            .Take(2)
            .ToListAsync();

        if (possibleTargets.Count == 1)
        {
            return possibleTargets[0].Id;
        }

        return null;
    }
}
