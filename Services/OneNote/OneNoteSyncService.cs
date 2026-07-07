using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

/*
================================================================================
                              OneNote synchronization
================================================================================
 This service contains the write side of the OneNote flow:
 - create and rename OneNote sections;
 - change the content of a OneNote page;
 - attach a file to a OneNote page;
 - synchronize one Node with its OneNote page.

 Beginner note: moving notes between sections or notebooks is intentionally not
 implemented here because that was excluded from the first version.
================================================================================
*/
public class OneNoteSyncService
{
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly OneNoteTokenStore tokenStore;
    private readonly HttpClient httpClient;

    public OneNoteSyncService(
        KnowledgeDbContext knowledgeDbContext,
        OneNoteTokenStore tokenStore,
        HttpClient httpClient)
    {
        this.knowledgeDbContext = knowledgeDbContext;
        this.tokenStore = tokenStore;
        this.httpClient = httpClient;
    }

    // Creates a new section inside an existing OneNote notebook.
    public async Task<OneNoteSectionResultDto> CreateSectionAsync(OneNoteCreateSectionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NotebookId))
        {
            throw new InvalidOperationException("NotebookId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("DisplayName is required.");
        }

        string accessToken = GetAccessToken(request.AccessToken);
        string notebookId = Uri.EscapeDataString(request.NotebookId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/notebooks/{notebookId}/sections";
        string json = JsonSerializer.Serialize(new { displayName = request.DisplayName });

        using HttpRequestMessage graphRequest = CreateJsonRequest(HttpMethod.Post, url, accessToken, json);
        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not create the OneNote section. Detail: {responseText}");
        }

        using JsonDocument document = JsonDocument.Parse(responseText);
        JsonElement root = document.RootElement;

        return new OneNoteSectionResultDto
        {
            SectionId = GetJsonString(root, "id"),
            DisplayName = GetJsonString(root, "displayName"),
            Message = "Section created in OneNote."
        };
    }

    // Renames an existing OneNote section.
    public async Task<OneNoteSectionResultDto> RenameSectionAsync(OneNoteRenameSectionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SectionId))
        {
            throw new InvalidOperationException("SectionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("DisplayName is required.");
        }

        string accessToken = GetAccessToken(request.AccessToken);
        string sectionId = Uri.EscapeDataString(request.SectionId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/sections/{sectionId}";
        string json = JsonSerializer.Serialize(new { displayName = request.DisplayName });

        using HttpRequestMessage graphRequest = CreateJsonRequest(HttpMethod.Patch, url, accessToken, json);
        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not rename the OneNote section. Detail: {responseText}");
        }

        return new OneNoteSectionResultDto
        {
            SectionId = request.SectionId,
            DisplayName = request.DisplayName,
            Message = "Section renamed in OneNote."
        };
    }

    // Replaces the title and body of one OneNote page.
    public async Task<OneNoteWriteResultDto> UpdatePageAsync(OneNoteUpdatePageRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PageId))
        {
            throw new InvalidOperationException("PageId is required.");
        }

        string accessToken = GetAccessToken(request.AccessToken);
        await UpdateOneNotePageFromValuesAsync(accessToken, request.PageId, request.Title, request.HtmlContent);

        return new OneNoteWriteResultDto
        {
            PageId = request.PageId,
            Message = "OneNote page changed."
        };
    }

    // Adds a file attachment to one OneNote page.
    public async Task<OneNoteWriteResultDto> AttachFileAsync(OneNoteAttachFileRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PageId))
        {
            throw new InvalidOperationException("PageId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new InvalidOperationException("FileName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Base64Content))
        {
            throw new InvalidOperationException("Base64Content is required.");
        }

        string accessToken = GetAccessToken(request.AccessToken);
        byte[] fileBytes = Convert.FromBase64String(request.Base64Content);
        await AttachFileToPageAsync(accessToken, request.PageId, request.FileName, request.ContentType, fileBytes);

        return new OneNoteWriteResultDto
        {
            PageId = request.PageId,
            Message = "File associated with the OneNote page."
        };
    }

    // Synchronizes one Node with the OneNote page stored in Node.ExternalId.
    public async Task<OneNoteSyncResultDto> SynchronizeNodeAsync(int nodeId, OneNoteSyncRequestDto request)
    {
        await CreateSyncTableIfMissingAsync();

        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(item => item.Id == nodeId);

        if (node is null)
        {
            throw new InvalidOperationException("Node not found.");
        }

        OneNoteSyncState state = await FindOrCreateStateAsync(node);
        string accessToken = GetAccessToken(request.AccessToken);
        OneNotePageInfo page = await ReadOneNotePageAsync(accessToken, state.OneNotePageId);
        OneNotePageImport? importRow = await FindOneNoteImportRowAsync(page.PageId);
        FillMissingBaselineDates(state, node, importRow);

        bool copiedFromOneNoteToNode = false;
        bool copiedFromNodeToOneNote = false;
        DateTime now = DateTime.UtcNow;
        bool nodeDateChanged = state.NodeUpdateDate.HasValue && node.UpdateDate > state.NodeUpdateDate.Value;
        bool oneNoteDateChanged = state.OneNoteUpdateDate.HasValue && page.LastModifiedDateTime > state.OneNoteUpdateDate.Value;
        bool nodeValuesChanged = IsNodeDifferentFromLastImport(node, importRow);
        bool oneNoteValuesChanged = IsOneNoteDifferentFromLastImport(page, importRow);
        bool nodeChanged = nodeDateChanged || nodeValuesChanged;
        bool oneNoteChanged = oneNoteDateChanged || oneNoteValuesChanged;

        if (oneNoteChanged && nodeChanged)
        {
            state.Status = "SynchronizationFailure";
            await knowledgeDbContext.SaveChangesAsync();
            return CreateSyncResult(
                node,
                state,
                page,
                false,
                false,
                "OneNote and the Node were both changed after the last synchronization. The user must decide which version wins.");
        }

        if (oneNoteChanged)
        {
            await CopyOneNotePageToDatabaseAsync(page, node, now);
            copiedFromOneNoteToNode = true;
        }
        else if (nodeChanged)
        {
            await UpdateOneNotePageFromNodeAsync(accessToken, node, page.PageId);
            await RenameOneNoteSectionFromNodeAsync(accessToken, node, page);
            page = await ReadOneNotePageAsync(accessToken, state.OneNotePageId);
            await SaveOneNoteImportRowAsync(page);
            await RefreshOneNoteTreeRowsAsync(page, node, now);
            copiedFromNodeToOneNote = true;
        }

        state.LastSyncDate = now;
        state.NodeUpdateDate = node.UpdateDate;
        state.OneNoteUpdateDate = page.LastModifiedDateTime;
        state.Status = "Ok";
        string message = copiedFromOneNoteToNode
            ? "OneNote was updated and the Node was not. OneNote was copied to the database, including the staging row and tree rows."
            : copiedFromNodeToOneNote
                ? "The Node was updated and OneNote was not. The database was copied to OneNote."
                : "Nothing changed since the last synchronization.";

        await knowledgeDbContext.SaveChangesAsync();
        return CreateSyncResult(node, state, page, copiedFromOneNoteToNode, copiedFromNodeToOneNote, message);
    }

    // Synchronizes many OneNote Nodes. When limit is null, all OneNote Nodes
    // are synchronized. When limit has a value, only that number is processed.
    public async Task<OneNoteSyncManyResultDto> SynchronizeNodesAsync(OneNoteSyncRequestDto request, int? limit)
    {
        await CreateSyncTableIfMissingAsync();

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

        return result;
    }

    // Creates the synchronization table automatically for simple local setup.
    private async Task CreateSyncTableIfMissingAsync()
    {
        string sql = @"
IF OBJECT_ID('dbo.OneNoteSyncState', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OneNoteSyncState
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        nodeId INT NOT NULL,
        oneNotePageId NVARCHAR(200) NOT NULL,
        lastSyncDate DATETIME2 NULL,
        nodeUpdateDate DATETIME2 NULL,
        oneNoteUpdateDate DATETIME2 NULL,
        status NVARCHAR(50) NOT NULL
    );

    CREATE UNIQUE INDEX IX_OneNoteSyncState_NodeId ON dbo.OneNoteSyncState(nodeId);
END

IF OBJECT_ID('dbo.OneNoteSyncState', 'U') IS NOT NULL
BEGIN
    DECLARE @constraintName SYSNAME;
    DECLARE @dropConstraintSql NVARCHAR(MAX);

    IF COL_LENGTH('dbo.OneNoteSyncState', 'nodeUpdateDate') IS NULL
    BEGIN
        ALTER TABLE dbo.OneNoteSyncState ADD nodeUpdateDate DATETIME2 NULL;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'oneNoteUpdateDate') IS NULL
    BEGIN
        ALTER TABLE dbo.OneNoteSyncState ADD oneNoteUpdateDate DATETIME2 NULL;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'lastSyncedNodeUpdateDate') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.OneNoteSyncState SET nodeUpdateDate = lastSyncedNodeUpdateDate WHERE nodeUpdateDate IS NULL');
        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN lastSyncedNodeUpdateDate;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'lastSyncedOneNoteUpdateDate') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.OneNoteSyncState SET oneNoteUpdateDate = lastSyncedOneNoteUpdateDate WHERE oneNoteUpdateDate IS NULL');
        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN lastSyncedOneNoteUpdateDate;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'oneNoteSectionId') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN oneNoteSectionId;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'fileName') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN fileName;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'message') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN message;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'nodeDataHash') IS NOT NULL
    BEGIN
        SELECT @constraintName = defaultConstraints.name
        FROM sys.default_constraints defaultConstraints
        INNER JOIN sys.columns columns
            ON columns.default_object_id = defaultConstraints.object_id
        WHERE defaultConstraints.parent_object_id = OBJECT_ID('dbo.OneNoteSyncState')
            AND columns.name = 'nodeDataHash';

        IF @constraintName IS NOT NULL
        BEGIN
            SET @dropConstraintSql = N'ALTER TABLE dbo.OneNoteSyncState DROP CONSTRAINT ' + QUOTENAME(@constraintName);
            EXEC(@dropConstraintSql);
        END

        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN nodeDataHash;
    END

    IF COL_LENGTH('dbo.OneNoteSyncState', 'oneNoteDataHash') IS NOT NULL
    BEGIN
        SET @constraintName = NULL;

        SELECT @constraintName = defaultConstraints.name
        FROM sys.default_constraints defaultConstraints
        INNER JOIN sys.columns columns
            ON columns.default_object_id = defaultConstraints.object_id
        WHERE defaultConstraints.parent_object_id = OBJECT_ID('dbo.OneNoteSyncState')
            AND columns.name = 'oneNoteDataHash';

        IF @constraintName IS NOT NULL
        BEGIN
            SET @dropConstraintSql = N'ALTER TABLE dbo.OneNoteSyncState DROP CONSTRAINT ' + QUOTENAME(@constraintName);
            EXEC(@dropConstraintSql);
        END

        ALTER TABLE dbo.OneNoteSyncState DROP COLUMN oneNoteDataHash;
    END
END";

        await knowledgeDbContext.Database.ExecuteSqlRawAsync(sql);
    }

    // Finds the sync row for this Node, or creates it from Node.ExternalId.
    private async Task<OneNoteSyncState> FindOrCreateStateAsync(Node node)
    {
        OneNoteSyncState? state = await knowledgeDbContext.OneNoteSyncStates.FirstOrDefaultAsync(item => item.NodeId == node.Id);

        if (state is not null)
        {
            return state;
        }

        if (string.IsNullOrWhiteSpace(node.ExternalId))
        {
            throw new InvalidOperationException("This Node does not have a OneNote page id in ExternalId.");
        }

        state = new OneNoteSyncState
        {
            NodeId = node.Id,
            OneNotePageId = node.ExternalId,
            Status = "Ok"
        };

        knowledgeDbContext.OneNoteSyncStates.Add(state);
        await knowledgeDbContext.SaveChangesAsync();

        return state;
    }

    // Finds the staging row imported for this OneNote page.
    private async Task<OneNotePageImport?> FindOneNoteImportRowAsync(string pageId)
    {
        return await knowledgeDbContext.OneNotePageImports
            .FirstOrDefaultAsync(row => row.GraphPageId == pageId);
    }

    // Gives a new sync row useful baseline dates. This makes the first sync
    // able to detect changes that happened after import/processing.
    private static void FillMissingBaselineDates(OneNoteSyncState state, Node node, OneNotePageImport? importRow)
    {
        if (state.NodeUpdateDate is null)
        {
            state.NodeUpdateDate = node.UpdateDate;
        }

        if (state.OneNoteUpdateDate is null)
        {
            state.OneNoteUpdateDate = importRow?.LastModifiedDateTime ?? DateTime.MinValue;
        }
    }

    // Reads one OneNote page and its HTML content from Microsoft Graph.
    private async Task<OneNotePageInfo> ReadOneNotePageAsync(string accessToken, string pageId)
    {
        string safePageId = Uri.EscapeDataString(pageId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages/{safePageId}?$select=id,title,createdDateTime,lastModifiedDateTime,links,parentSection,parentNotebook&$expand=parentSection,parentNotebook";

        using HttpRequestMessage graphRequest = new HttpRequestMessage(HttpMethod.Get, url);
        graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not read the OneNote page. Detail: {responseText}");
        }

        using JsonDocument document = JsonDocument.Parse(responseText);
        JsonElement root = document.RootElement;

        string html = await ReadPageContentAsync(accessToken, pageId);

        return new OneNotePageInfo
        {
            PageId = GetJsonString(root, "id"),
            Title = GetJsonString(root, "title"),
            HtmlContent = html,
            TextContent = ConvertHtmlToText(html),
            CreatedDateTime = GetJsonDate(root, "createdDateTime"),
            LastModifiedDateTime = GetJsonDate(root, "lastModifiedDateTime"),
            WebUrl = ReadWebUrl(root),
            SectionId = ReadNestedString(root, "parentSection", "id"),
            SectionName = ReadNestedString(root, "parentSection", "displayName"),
            NotebookId = ReadNestedString(root, "parentNotebook", "id"),
            NotebookName = ReadNestedString(root, "parentNotebook", "displayName")
        };
    }

    // Reads the HTML body of one OneNote page.
    private async Task<string> ReadPageContentAsync(string accessToken, string pageId)
    {
        string safePageId = Uri.EscapeDataString(pageId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages/{safePageId}/content";

        using HttpRequestMessage graphRequest = new HttpRequestMessage(HttpMethod.Get, url);
        graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not read OneNote page content. Detail: {responseText}");
        }

        return responseText;
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
        OneNotePageImport? importRow = await knowledgeDbContext.OneNotePageImports
            .FirstOrDefaultAsync(row => row.GraphPageId == page.PageId);

        if (importRow is null)
        {
            importRow = new OneNotePageImport
            {
                GraphPageId = page.PageId
            };

            knowledgeDbContext.OneNotePageImports.Add(importRow);
        }

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

    // If the database changed the note's section name, try to rename the
    // OneNote section. This does not move the note to another section.
    private async Task RenameOneNoteSectionFromNodeAsync(string accessToken, Node node, OneNotePageInfo page)
    {
        string sectionNameFromNode = node.Par2 ?? string.Empty;

        if (string.IsNullOrWhiteSpace(page.SectionId)
            || string.IsNullOrWhiteSpace(sectionNameFromNode)
            || SameText(sectionNameFromNode, page.SectionName))
        {
            return;
        }

        OneNoteRenameSectionRequestDto request = new OneNoteRenameSectionRequestDto
        {
            AccessToken = accessToken,
            SectionId = page.SectionId,
            DisplayName = sectionNameFromNode
        };

        await RenameSectionAsync(request);
    }

    // Copies the database Node into the OneNote page.
    private async Task UpdateOneNotePageFromNodeAsync(string accessToken, Node node, string pageId)
    {
        string html = $"<div>{WebUtility.HtmlEncode(node.Description)}</div>";
        await UpdateOneNotePageFromValuesAsync(accessToken, pageId, node.Reference, html);
    }

    // Sends the OneNote PATCH command that changes title and body.
    private async Task UpdateOneNotePageFromValuesAsync(string accessToken, string pageId, string title, string htmlContent)
    {
        string safePageId = Uri.EscapeDataString(pageId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages/{safePageId}/content";
        string bodyHtml = string.IsNullOrWhiteSpace(htmlContent) ? "<div></div>" : htmlContent;

        object[] commands =
        {
            new { target = "title", action = "replace", content = title },
            new { target = "body", action = "replace", content = bodyHtml }
        };

        string json = JsonSerializer.Serialize(commands);
        using HttpRequestMessage graphRequest = CreateJsonRequest(HttpMethod.Patch, url, accessToken, json);
        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not change the OneNote page. Detail: {responseText}");
        }
    }

    // Sends a multipart PATCH command that appends a file object to the page.
    private async Task AttachFileToPageAsync(string accessToken, string pageId, string fileName, string contentType, byte[] fileBytes)
    {
        string safePageId = Uri.EscapeDataString(pageId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages/{safePageId}/content";
        string safeFileName = WebUtility.HtmlEncode(fileName);
        string objectHtml = $"<object data-attachment=\"{safeFileName}\" data=\"name:fileBlock\" type=\"{WebUtility.HtmlEncode(contentType)}\" />";
        object[] commands = { new { target = "body", action = "append", content = objectHtml } };

        using MultipartFormDataContent multipart = new MultipartFormDataContent();
        StringContent commandContent = new StringContent(JsonSerializer.Serialize(commands), Encoding.UTF8, "application/json");
        ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        multipart.Add(commandContent, "Commands");
        multipart.Add(fileContent, "fileBlock", fileName);

        using HttpRequestMessage graphRequest = new HttpRequestMessage(HttpMethod.Patch, url);
        graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        graphRequest.Content = multipart;

        using HttpResponseMessage response = await httpClient.SendAsync(graphRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not associate the file with the OneNote page. Detail: {responseText}");
        }
    }

    private string GetAccessToken(string requestAccessToken)
    {
        if (!string.IsNullOrWhiteSpace(requestAccessToken))
        {
            return requestAccessToken;
        }

        string storedAccessToken = tokenStore.GetAccessToken();

        if (!string.IsNullOrWhiteSpace(storedAccessToken))
        {
            return storedAccessToken;
        }

        throw new InvalidOperationException("Login with Microsoft first, or send AccessToken in the request.");
    }

    private static HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, string accessToken, string json)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private static OneNoteSyncResultDto CreateSyncResult(
        Node node,
        OneNoteSyncState state,
        OneNotePageInfo page,
        bool copiedFromOneNoteToNode,
        bool copiedFromNodeToOneNote,
        string message)
    {
        return new OneNoteSyncResultDto
        {
            NodeId = node.Id,
            OneNotePageId = state.OneNotePageId,
            Status = state.Status,
            Message = message,
            CopiedFromOneNoteToNode = copiedFromOneNoteToNode,
            CopiedFromNodeToOneNote = copiedFromNodeToOneNote,
            LastSyncDate = state.LastSyncDate,
            NodeUpdateDate = node.UpdateDate,
            OneNoteUpdateDate = page.LastModifiedDateTime
        };
    }

    private static string ReadWebUrl(JsonElement pageJson)
    {
        if (!pageJson.TryGetProperty("links", out JsonElement links))
        {
            return string.Empty;
        }

        if (!links.TryGetProperty("oneNoteWebUrl", out JsonElement webUrl))
        {
            return string.Empty;
        }

        return GetJsonString(webUrl, "href");
    }

    private static string ReadNestedString(JsonElement element, string objectName, string propertyName)
    {
        if (!element.TryGetProperty(objectName, out JsonElement nested))
        {
            return string.Empty;
        }

        return GetJsonString(nested, propertyName);
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static DateTime GetJsonDate(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetDateTime(out DateTime value))
        {
            return value;
        }

        return DateTime.UtcNow;
    }

    private static string ConvertHtmlToText(string html)
    {
        string withoutScripts = Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withSpaces = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withSpaces);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static string LimitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength);
    }

    private static bool SameText(string? first, string? second)
    {
        return string.Equals(first ?? string.Empty, second ?? string.Empty, StringComparison.Ordinal);
    }

    // Compares the current database Node with the last values saved in
    // OneNotePageImport. This detects manual SQL edits even when updateDate
    // was not changed by the person editing the database.
    private static bool IsNodeDifferentFromLastImport(Node node, OneNotePageImport? importRow)
    {
        if (importRow is null)
        {
            return false;
        }

        return !SameText(node.Reference, importRow.PageTitle)
            || !SameText(node.Description, importRow.ContentText)
            || !SameText(node.Par1, importRow.NotebookName)
            || !SameText(node.Par2, importRow.SectionName)
            || !SameText(node.Link, importRow.WebUrl);
    }

    // Compares the current OneNote page with the last values saved in
    // OneNotePageImport. This lets the sync detect OneNote edits by values too,
    // not only by the lastModifiedDateTime returned by Microsoft Graph.
    private static bool IsOneNoteDifferentFromLastImport(OneNotePageInfo page, OneNotePageImport? importRow)
    {
        if (importRow is null)
        {
            return false;
        }

        return !SameText(page.Title, importRow.PageTitle)
            || !SameText(page.TextContent, importRow.ContentText)
            || !SameText(page.NotebookName, importRow.NotebookName)
            || !SameText(page.SectionName, importRow.SectionName)
            || !SameText(page.WebUrl, importRow.WebUrl);
    }

    private class OneNotePageInfo
    {
        public string PageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public string WebUrl { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string NotebookId { get; set; } = string.Empty;
        public string NotebookName { get; set; } = string.Empty;
    }
}
