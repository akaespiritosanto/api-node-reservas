using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                  Context saving
    ============================================================================
     Context rows can do two simple jobs:
     - store extra values connected to a Node, such as status or notebook name;
     - place Nodes inside a tree, using parent = 0 for the root Context.
    ============================================================================
    */

    private const string TreeContextDescriptionType = "tree";

    // Creates the Context rows for one Node.
    private void AddContexts(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
        // Location is the order of the context value for this Node: 1, 2, 3, ...
        int location = 1;

        foreach (string contextValue in record.Contexts)
        {
            if (string.IsNullOrWhiteSpace(contextValue))
            {
                continue;
            }

            knowledgeDbContext.Contexts.Add(new Context
            {
                NodeId = node.Id,
                Description = LimitText(contextValue, 8000),
                Parent = 0,
                Location = location,
                Par1 = LimitNullableText(record.ContextPar1, 200),
                UpdateDate = updateDate,
                DescriptionType = LimitNullableText(record.ContextDescriptionType, 10)
            });

            location++;
            result.ContextsCreated++;
        }
    }

    // Creates Context rows that place this Node under its parent Nodes in the tree.
    private async Task AddContextTreeRowsAsync(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
        foreach (string parentIdText in record.Parent)
        {
            if (string.IsNullOrWhiteSpace(parentIdText))
            {
                continue;
            }

            int? parentNodeId = await FindTargetNodeIdAsync(parentIdText, record.ParentType);

            if (parentNodeId is null)
            {
                logger.LogWarning(
                    "Context parent was not found or is ambiguous. Child node: {ChildNodeId}, ParentId: {ParentId}, ParentType: {ParentType}.",
                    node.Id,
                    parentIdText,
                    record.ParentType);

                continue;
            }

            Context parentContext = await FindOrCreateRootContextAsync(parentNodeId.Value, updateDate, result);

            knowledgeDbContext.Contexts.Add(new Context
            {
                NodeId = node.Id,
                Description = LimitText(node.Reference, 8000),
                Parent = parentContext.Id,
                Location = 0,
                Par1 = null,
                UpdateDate = updateDate,
                DescriptionType = TreeContextDescriptionType
            });

            result.ContextsCreated++;
        }
    }

    // Gets the root Context for a parent Node, or creates it if this is the first child.
    private async Task<Context> FindOrCreateRootContextAsync(int parentNodeId, DateTime updateDate, SaveResult result)
    {
        Context? parentContext = await knowledgeDbContext.Contexts.FirstOrDefaultAsync(context =>
            context.NodeId == parentNodeId
            && context.Parent == 0
            && context.DescriptionType == TreeContextDescriptionType);

        if (parentContext is not null)
        {
            return parentContext;
        }

        Node? parentNode = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(node => node.Id == parentNodeId);

        parentContext = new Context
        {
            NodeId = parentNodeId,
            Description = LimitText(parentNode?.Reference ?? string.Empty, 8000),
            Parent = 0,
            Location = 0,
            Par1 = null,
            UpdateDate = updateDate,
            DescriptionType = TreeContextDescriptionType
        };

        knowledgeDbContext.Contexts.Add(parentContext);
        await knowledgeDbContext.SaveChangesAsync();
        result.ContextsCreated++;

        return parentContext;
    }
}
