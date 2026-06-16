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
            // Create an arc relation from this node to the parent node. The
            // relation type is "parent" and the target id is the parent
            // value. AddArcAsync will search the database to find the Node id
            // that corresponds to the provided parent text.
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
        // If there is no target id text, nothing to do.
        if (string.IsNullOrWhiteSpace(targetIdText))
        {
            return;
        }

        // Try to find the Node database id that matches the provided
        // target text and optional type. If the lookup is ambiguous or
        // missing, we skip creating the relation to avoid incorrect links.
        int? targetNodeId = await FindTargetNodeIdAsync(targetIdText, targetType);

        if (targetNodeId is null)
        {
            // The Node is still saved but the relation is skipped because
            // we cannot find a unique safe target.
            logger.LogWarning(
                "Arc target was not found or is ambiguous. Source node: {SourceNodeId}, TargetId: {TargetId}, TargetType: {TargetType}.",
                node.Id,
                targetIdText,
                targetType);

            return;
        }

        // Create the Arc row linking the two Nodes.
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
        // If a type is provided in the mapping, we require both type id and
        // type name to match exactly. This is the safest lookup.
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            Node? targetNode = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(node =>
                node.TypeId == targetTypeId && node.Type == targetType);

            return targetNode?.Id;
        }

        // When no type is provided the matching is ambiguous: we look for
        // nodes that have the same TypeId and only accept a single match.
        // If there are 0 or multiple matches, we return null to indicate
        // the relation is unsafe.
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
