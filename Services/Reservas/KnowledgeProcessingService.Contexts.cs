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

            // Create a Context row that stores an extra value for this node.
            // Example: a status, a tag or the notebook name. These are not
            // part of the Node itself but additional information stored as
            // Context rows. Parent = 0 indicates this is a non-tree context.
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
        foreach (KnowledgeParentDto parentMapping in record.ParentMappings)
        {
            await AddMappedContextTreeRowsAsync(node, parentMapping, updateDate, result);
        }

        foreach (string parentIdText in record.Parent)
        {
            if (string.IsNullOrWhiteSpace(parentIdText))
            {
                continue;
            }

            // Backwards-compatible simple parent handling: when the mapping
            // provided a plain parent id we try to find the Node that matches
            // that id and ParentType. If found, we attach a context row that
            // places this node under the parent node's root context.
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

            // Get or create the root context for the parent Node (notebook or other).
            Context parentContext = await FindOrCreateRootContextAsync(parentNodeId.Value, updateDate, result);

            // Create a tree context row that links this node as a child of the
            // parent's root context. DescriptionType = "tree" marks it as part
            // of the tree structure rather than an arbitrary context value.
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

    // Creates the notebook -> section -> note tree described by the parent mapping.
    private async Task AddMappedContextTreeRowsAsync(Node node, KnowledgeParentDto parentMapping, DateTime updateDate, SaveResult result)
    {
        if (string.IsNullOrWhiteSpace(parentMapping.FieldName))
        {
            return;
        }

        Node parentNode = await FindOrCreateContextNodeAsync(
            parentMapping.FieldName,
            parentMapping.FieldId,
            parentMapping.ParentType,
            parentMapping.ParentTypeId,
            updateDate,
            result);

        Context parentContext = await FindOrCreateRootContextAsync(parentNode.Id, updateDate, result);
        Context targetParentContext = parentContext;

        if (!string.IsNullOrWhiteSpace(parentMapping.GroupBy))
        {
            Node groupNode = await FindOrCreateContextNodeAsync(
                parentMapping.GroupBy,
                parentMapping.GroupById,
                parentMapping.GroupByType,
                parentMapping.GroupByTypeId,
                updateDate,
                result);

            targetParentContext = await FindOrCreateChildContextAsync(
                groupNode.Id,
                groupNode.Reference,
                parentContext.Id,
                updateDate,
                result);
        }

        await FindOrCreateChildContextAsync(node.Id, node.Reference, targetParentContext.Id, updateDate, result);
    }

    // Creates or updates the Node used by a parent Context, such as a notebook or section.
    private async Task<Node> FindOrCreateContextNodeAsync(
        string reference,
        string externalId,
        string type,
        int typeId,
        DateTime updateDate,
        SaveResult result)
    {
        string safeReference = LimitText(reference, 1000);
        string safeExternalId = LimitText(GetExternalIdOrReference(externalId, reference), 200);
        string safeType = LimitText(type, 30);

        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(existingNode =>
            existingNode.TypeId == typeId
            && existingNode.Type == safeType
            && existingNode.ExternalId == safeExternalId);

        if (node is null)
        {
            node = new Node
            {
                Reference = safeReference,
                TypeId = typeId,
                Type = safeType,
                ExternalId = safeExternalId
            };

            knowledgeDbContext.Nodes.Add(node);
        }

        node.Reference = safeReference;
        node.TypeId = typeId;
        node.Type = safeType;
        node.Description = safeReference;
        node.ExternalId = safeExternalId;
        node.Security = 0;
        node.UpdateDate = updateDate;
        node.UpdateUser = 0;

        await SaveNodeChangesAsync();

        return node;
    }

    // Uses the real external id when it exists; otherwise the reference is stable enough.
    private static string GetExternalIdOrReference(string externalId, string reference)
    {
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            return externalId;
        }

        return reference;
    }

    // Gets one child Context under a known parent Context, or creates it when missing.
    private async Task<Context> FindOrCreateChildContextAsync(
        int nodeId,
        string reference,
        int parentContextId,
        DateTime updateDate,
        SaveResult result)
    {
        Context? childContext = await knowledgeDbContext.Contexts.FirstOrDefaultAsync(context =>
            context.NodeId == nodeId
            && context.Parent == parentContextId
            && context.DescriptionType == TreeContextDescriptionType);

        if (childContext is not null)
        {
            childContext.Description = LimitText(reference, 8000);
            childContext.UpdateDate = updateDate;
            return childContext;
        }

        childContext = new Context
        {
            NodeId = nodeId,
            Description = LimitText(reference, 8000),
            Parent = parentContextId,
            Location = 0,
            Par1 = null,
            UpdateDate = updateDate,
            DescriptionType = TreeContextDescriptionType
        };

        knowledgeDbContext.Contexts.Add(childContext);
        await SaveContextAndArcChangesAsync();
        result.ContextsCreated++;

        return childContext;
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

        // Create a root context for the parent Node when it does not exist
        // yet. This becomes the context that child contexts will point to.
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
        await SaveContextAndArcChangesAsync();
        result.ContextsCreated++;

        return parentContext;
    }
}
