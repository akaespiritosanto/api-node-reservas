using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
    // Synchronizes one Node with the OneNote page stored in Node.ExternalId.
    public async Task<OneNoteSyncResultDto> SynchronizeNodeAsync(int nodeId, OneNoteSyncRequestDto request)
    {
        await EnsureNodeSyncColumnsAsync();

        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(item => item.Id == nodeId);

        if (node is null)
        {
            throw new InvalidOperationException("Node not found.");
        }

        if (string.IsNullOrWhiteSpace(node.ExternalId))
        {
            throw new InvalidOperationException("This Node does not have a OneNote page id in ExternalId.");
        }

        string accessToken = GetAccessToken(request.AccessToken);
        OneNotePageInfo page = await ReadOneNotePageAsync(accessToken, node.ExternalId);
        OneNotePageImport? importRow = await FindOneNoteImportRowAsync(page.PageId);
        FillMissingNodeSyncDates(node, importRow);

        DateTime now = DateTime.UtcNow;
        bool nodeChanged = WasNodeChangedAfterLastSync(node);
        bool oneNoteChanged = WasOneNoteChangedAfterLastSync(node, page, importRow);

        if (!nodeChanged && !OneNotePageMatchesNode(page, node))
        {
            oneNoteChanged = true;
        }

        if (oneNoteChanged && nodeChanged)
        {
            node.SyncStatus = "SynchronizationFailure";
            await knowledgeDbContext.SaveChangesAsync();

            return CreateSyncResult(
                node,
                page,
                "SynchronizationFailure",
                false,
                false,
                "OneNote and the Node were both changed after the last synchronization. The user must decide which version wins.");
        }

        bool copiedFromOneNoteToNode = false;
        bool copiedFromNodeToOneNote = false;

        if (oneNoteChanged)
        {
            await CopyOneNotePageToDatabaseAsync(page, node, now);
            copiedFromOneNoteToNode = true;
        }
        else if (nodeChanged)
        {
            await UpdateOneNotePageFromNodeAsync(accessToken, node, page.PageId);
            await RenameOneNoteSectionFromNodeAsync(accessToken, node, page);
            page = await ReadOneNotePageAfterNodeUpdateAsync(accessToken, node, page);
            await SaveOneNoteImportRowFromNodeAsync(node, page, now);
            await RefreshOneNoteTreeRowsAsync(page, node, now);
            copiedFromNodeToOneNote = true;
        }

        node.LastModifiedDateTime = page.LastModifiedDateTime;
        node.ImportedAt = now;
        node.SyncStatus = "Ok";

        string message = CreateSynchronizationMessage(copiedFromOneNoteToNode, copiedFromNodeToOneNote);

        await knowledgeDbContext.SaveChangesAsync();
        return CreateSyncResult(node, page, "Ok", copiedFromOneNoteToNode, copiedFromNodeToOneNote, message);
    }

    // Synchronizes many OneNote Nodes. When limit is null, all OneNote Nodes
    // are synchronized. When limit has a value, only that number is processed.
    public async Task<OneNoteSyncManyResultDto> SynchronizeNodesAsync(OneNoteSyncRequestDto request, int? limit)
    {
        await EnsureNodeSyncColumnsAsync();

        IQueryable<Node> query = knowledgeDbContext.Nodes
            .Where(node => node.Type == "OneNotePage" && node.ExternalId != "")
            .OrderBy(node => node.Id);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        List<Node> nodes = await query.ToListAsync();
        OneNoteSyncManyResultDto result = new OneNoteSyncManyResultDto
        {
            NodesFound = nodes.Count
        };

        foreach (Node node in nodes)
        {
            await TrySynchronizeOneNodeInBatchAsync(node, request, result);
        }

        return result;
    }

    private async Task TrySynchronizeOneNodeInBatchAsync(
        Node node,
        OneNoteSyncRequestDto request,
        OneNoteSyncManyResultDto result)
    {
        try
        {
            OneNoteSyncResultDto nodeResult = await SynchronizeNodeAsync(node.Id, request);
            result.Results.Add(nodeResult);
            result.NodesSynchronized++;

            if (nodeResult.CopiedFromOneNoteToNode)
            {
                result.CopiedFromOneNoteToNode++;
            }

            if (nodeResult.CopiedFromNodeToOneNote)
            {
                result.CopiedFromNodeToOneNote++;
            }

            if (nodeResult.Status == "SynchronizationFailure")
            {
                result.Conflicts++;
            }
        }
        catch (Exception exception)
        {
            // One bad OneNote page should not stop the rest of the batch.
            result.Errors++;
            result.Results.Add(new OneNoteSyncResultDto
            {
                NodeId = node.Id,
                OneNotePageId = node.ExternalId,
                Status = "Error",
                Message = exception.Message,
                NodeUpdateDate = node.UpdateDate
            });
        }
    }

    private static bool WasNodeChangedAfterLastSync(Node node)
    {
        DateTime lastSyncDate = node.ImportedAt ?? node.UpdateDate;
        return node.UpdateDate > lastSyncDate;
    }

    private static bool WasOneNoteChangedAfterLastSync(Node node, OneNotePageInfo page, OneNotePageImport? importRow)
    {
        DateTime lastOneNoteUpdateDate = node.LastModifiedDateTime ?? DateTime.MinValue;
        bool pageDateChanged = page.LastModifiedDateTime > lastOneNoteUpdateDate;
        bool stagingDateChanged = importRow is not null && importRow.LastModifiedDateTime > lastOneNoteUpdateDate;
        return pageDateChanged || stagingDateChanged;
    }

    private static string CreateSynchronizationMessage(bool copiedFromOneNoteToNode, bool copiedFromNodeToOneNote)
    {
        if (copiedFromOneNoteToNode)
        {
            return "OneNote was updated and the Node was not. OneNote was copied to the database, including the staging row and tree rows.";
        }

        if (copiedFromNodeToOneNote)
        {
            return "The Node was updated and OneNote was not. The database was copied to OneNote.";
        }

        return "Nothing changed since the last synchronization.";
    }

    private static OneNoteSyncResultDto CreateSyncResult(
        Node node,
        OneNotePageInfo page,
        string status,
        bool copiedFromOneNoteToNode,
        bool copiedFromNodeToOneNote,
        string message)
    {
        return new OneNoteSyncResultDto
        {
            NodeId = node.Id,
            OneNotePageId = node.ExternalId,
            Status = status,
            Message = message,
            CopiedFromOneNoteToNode = copiedFromOneNoteToNode,
            CopiedFromNodeToOneNote = copiedFromNodeToOneNote,
            LastSyncDate = node.ImportedAt,
            NodeUpdateDate = node.UpdateDate,
            OneNoteUpdateDate = page.LastModifiedDateTime
        };
    }
}
