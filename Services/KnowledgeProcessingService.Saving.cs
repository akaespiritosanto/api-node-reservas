using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                               Knowledge DB saving
    ============================================================================
     This part writes the converted record to the knowledge database:
     - Node: the main object.
     - Context: extra text connected to the node.
     - Arc: a relation from this node to another node.
    ============================================================================
    */
    private async Task<SaveResult> SaveKnowledgeRecordAsync(KnowledgeRecordDto record)
    {
        DateTime now = DateTime.UtcNow;
        SaveResult result = new();
        int typeId = ToInt(record.IdInformacao);
        string type = LimitText(record.Tipo, 30);

        Node? node = await knowledgeDbContext.Nodes
            .FirstOrDefaultAsync(existingNode => existingNode.TypeId == typeId && existingNode.Type == type);

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

        FillNode(node, record, now);

        try
        {
            await knowledgeDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Erro ao gravar o Node na base de conhecimento. Confirma se a tabela Node aceita TypeId + Type como identificador e se ja nao existe uma restricao antiga unica no campo reference.",
                exception);
        }

        List<Context> oldContexts = await knowledgeDbContext.Contexts.Where(context => context.NodeId == node.Id).ToListAsync();
        List<Arc> oldArcs = await knowledgeDbContext.Arcs.Where(arc => arc.Source == node.Id).ToListAsync();

        knowledgeDbContext.Contexts.RemoveRange(oldContexts);
        knowledgeDbContext.Arcs.RemoveRange(oldArcs);

        AddContexts(node, record, now, result);
        await AddArcsAsync(node, record, now, result);

        try
        {
            await knowledgeDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Erro ao gravar Contexts ou Arcs na base de conhecimento. Confirma se os campos source, target, typeId e nodeId aceitam os valores gerados pelo processamento.",
                exception);
        }

        return result;
    }

    private void AddContexts(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
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
                Location = location,
                Par1 = null,
                UpdateDate = updateDate,
                DescriptionType = null
            });

            location++;
            result.ContextsCreated++;
        }
    }

    private async Task AddArcsAsync(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
        foreach (string parent in record.Parent)
        {
            await AddArcAsync(node, "parent", parent, record.TipoE, updateDate, result);
        }

        foreach (KnowledgeRelationDto relation in record.Relations)
        {
            await AddArcAsync(node, relation.Type, relation.TargetId, relation.TargetType, updateDate, result);
        }
    }

    private async Task AddArcAsync(
        Node node,
        string relationType,
        string targetIdText,
        string targetType,
        DateTime updateDate,
        SaveResult result)
    {
        if (string.IsNullOrWhiteSpace(targetIdText))
        {
            return;
        }

        int? targetNodeId = await FindTargetNodeIdAsync(targetIdText, targetType);

        if (targetNodeId is null)
        {
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
            TypeId = 0,
            Type = LimitText(relationType, 50),
            UpdateDate = updateDate
        });

        result.ArcsCreated++;
    }

    private async Task<int?> FindTargetNodeIdAsync(string targetIdText, string targetType)
    {
        int targetTypeId = ToInt(targetIdText);

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            Node? targetNode = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(node =>
                node.TypeId == targetTypeId && node.Type == targetType);

            return targetNode?.Id;
        }

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

    private static void FillNode(Node node, KnowledgeRecordDto record, DateTime updateDate)
    {
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
