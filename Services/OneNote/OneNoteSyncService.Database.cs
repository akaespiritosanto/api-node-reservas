using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
    // Adds the OneNote synchronization dates to Node when an older database is used.
    private async Task EnsureNodeSyncColumnsAsync()
    {
        string sql = @"
IF COL_LENGTH('dbo.Node', 'LastModifiedDateTime') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD LastModifiedDateTime DATETIME2 NULL;
END

IF COL_LENGTH('dbo.Node', 'ImportedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD ImportedAt DATETIME2 NULL;
END

IF COL_LENGTH('dbo.Node', 'syncStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD syncStatus VARCHAR(50) NULL;
END";

        await knowledgeDbContext.Database.ExecuteSqlRawAsync(sql);
    }

    // Finds the staging row imported for this OneNote page.
    private async Task<OneNotePageImport?> FindOneNoteImportRowAsync(string pageId)
    {
        return await knowledgeDbContext.OneNotePageImports
            .FirstOrDefaultAsync(row => row.GraphPageId == pageId);
    }

    // Gives the Node useful baseline dates. This makes the first sync
    // able to detect changes that happened after import/processing.
    private static void FillMissingNodeSyncDates(Node node, OneNotePageImport? importRow)
    {
        if (node.ImportedAt is null)
        {
            node.ImportedAt = node.UpdateDate;
        }

        if (node.LastModifiedDateTime is null)
        {
            node.LastModifiedDateTime = importRow?.LastModifiedDateTime ?? DateTime.MinValue;
        }
    }

    // Copies the OneNote values into the database tables used by OneNote data.
    private async Task CopyOneNotePageToDatabaseAsync(OneNotePageInfo page, Node node, DateTime updateDate)
    {
        CopyOneNotePageToNode(page, node, updateDate);
        await SaveOneNoteImportRowAsync(page);
        await RefreshOneNoteTreeRowsAsync(page, node, updateDate);
        await knowledgeDbContext.SaveChangesAsync();
    }

    // Copies the OneNote values into the main note Node.
    private static void CopyOneNotePageToNode(OneNotePageInfo page, Node node, DateTime updateDate)
    {
        node.Reference = LimitText(page.Title, 1000);
        node.Description = LimitText(page.TextContent, 8000);
        node.Par1 = LimitText(page.NotebookName, 200);
        node.Par2 = LimitText(page.SectionName, 200);
        node.Link = LimitText(page.WebUrl, 500);
        node.ExternalId = LimitText(page.PageId, 200);
        node.UpdateDate = updateDate;
    }

    // Updates the OneNotePageImport row so the staging table matches OneNote too.
    private async Task SaveOneNoteImportRowAsync(OneNotePageInfo page)
    {
        OneNotePageImport importRow = await FindOrCreateImportRowAsync(page.PageId);

        importRow.NotebookId = page.NotebookId;
        importRow.NotebookName = page.NotebookName;
        importRow.SectionId = page.SectionId;
        importRow.SectionName = page.SectionName;
        importRow.PageTitle = page.Title;
        importRow.ContentText = page.TextContent;
        importRow.ContentHtml = page.HtmlContent;
        importRow.CreatedDateTime = page.CreatedDateTime;
        importRow.LastModifiedDateTime = page.LastModifiedDateTime;
        importRow.WebUrl = page.WebUrl;
        importRow.ImportedAt = DateTime.UtcNow;
    }

    // Updates the staging row after the database wins the sync decision.
    // This makes OneNotePageImport become the new "last synchronized copy".
    private async Task SaveOneNoteImportRowFromNodeAsync(Node node, OneNotePageInfo page, DateTime importedAt)
    {
        OneNotePageImport importRow = await FindOrCreateImportRowAsync(page.PageId);

        importRow.NotebookId = page.NotebookId;
        importRow.NotebookName = node.Par1 ?? page.NotebookName;
        importRow.SectionId = page.SectionId;
        importRow.SectionName = node.Par2 ?? page.SectionName;
        importRow.PageTitle = node.Reference;
        importRow.ContentText = node.Description;
        importRow.ContentHtml = CreateHtmlFromNodeDescription(node);
        importRow.CreatedDateTime = page.CreatedDateTime;
        importRow.LastModifiedDateTime = page.LastModifiedDateTime;
        importRow.WebUrl = string.IsNullOrWhiteSpace(node.Link) ? page.WebUrl : node.Link;
        importRow.ImportedAt = importedAt;
    }

    private async Task<OneNotePageImport> FindOrCreateImportRowAsync(string pageId)
    {
        OneNotePageImport? importRow = await knowledgeDbContext.OneNotePageImports
            .FirstOrDefaultAsync(row => row.GraphPageId == pageId);

        if (importRow is not null)
        {
            return importRow;
        }

        importRow = new OneNotePageImport
        {
            GraphPageId = pageId
        };

        knowledgeDbContext.OneNotePageImports.Add(importRow);
        return importRow;
    }

    // Refreshes notebook/section Nodes and tree Context rows for this note.
    private async Task RefreshOneNoteTreeRowsAsync(OneNotePageInfo page, Node noteNode, DateTime updateDate)
    {
        Node notebookNode = await FindOrCreateTreeNodeAsync(page.NotebookName, page.NotebookId, "notebook", 3001, updateDate);
        Node sectionNode = await FindOrCreateTreeNodeAsync(page.SectionName, page.SectionId, "section", 3002, updateDate);
        await knowledgeDbContext.SaveChangesAsync();

        Context notebookRootContext = await FindOrCreateTreeContextAsync(notebookNode.Id, notebookNode.Reference, 0, updateDate);
        await knowledgeDbContext.SaveChangesAsync();

        Context sectionContext = await FindOrCreateTreeContextAsync(sectionNode.Id, sectionNode.Reference, notebookRootContext.Id, updateDate);
        await knowledgeDbContext.SaveChangesAsync();

        await RemoveOldNoteTreeContextsAsync(noteNode.Id, sectionContext.Id);
        await FindOrCreateTreeContextAsync(noteNode.Id, noteNode.Reference, sectionContext.Id, updateDate);
    }

    // Removes old tree positions for the note before creating the current one.
    // This keeps the note under the current OneNote section after a rename.
    private async Task RemoveOldNoteTreeContextsAsync(int noteNodeId, int currentSectionContextId)
    {
        List<Context> oldTreeContexts = await knowledgeDbContext.Contexts
            .Where(context => context.NodeId == noteNodeId
                && context.DescriptionType == "tree"
                && context.Location != currentSectionContextId)
            .ToListAsync();

        knowledgeDbContext.Contexts.RemoveRange(oldTreeContexts);
    }

    // Finds or creates the Node used in the OneNote tree, such as notebook or section.
    private async Task<Node> FindOrCreateTreeNodeAsync(string reference, string externalId, string type, int typeId, DateTime updateDate)
    {
        string safeExternalId = LimitText(string.IsNullOrWhiteSpace(externalId) ? reference : externalId, 200);
        string safeType = LimitText(type, 30);

        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(existingNode =>
            existingNode.TypeId == typeId
            && existingNode.Type == safeType
            && existingNode.ExternalId == safeExternalId);

        if (node is null)
        {
            node = new Node
            {
                TypeId = typeId,
                Type = safeType,
                ExternalId = safeExternalId
            };

            knowledgeDbContext.Nodes.Add(node);
        }

        node.Reference = LimitText(reference, 1000);
        node.Description = LimitText(reference, 8000);
        node.Security = 0;
        node.UpdateDate = updateDate;
        node.UpdateUser = 0;

        return node;
    }

    // Finds or creates one tree Context row and updates its description.
    private async Task<Context> FindOrCreateTreeContextAsync(int nodeId, string description, int parentContextId, DateTime updateDate)
    {
        Context? context = await knowledgeDbContext.Contexts.FirstOrDefaultAsync(existingContext =>
            existingContext.NodeId == nodeId
            && existingContext.Location == parentContextId
            && existingContext.DescriptionType == "tree");

        if (context is null)
        {
            context = new Context
            {
                NodeId = nodeId,
                Location = parentContextId,
                DescriptionType = "tree"
            };

            knowledgeDbContext.Contexts.Add(context);
        }

        context.Description = LimitText(description, 8000);
        context.UpdateDate = updateDate;

        return context;
    }
}
