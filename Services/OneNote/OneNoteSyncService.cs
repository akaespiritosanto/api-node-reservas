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
        await SaveAttachedFileNameAsync(request.PageId, request.FileName);

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

        bool copiedFromOneNoteToNode = false;
        bool copiedFromNodeToOneNote = false;
        DateTime now = DateTime.UtcNow;
        DateTime lastSyncDate = state.LastSyncDate ?? DateTime.MinValue;

        if (state.LastSyncDate is null)
        {
            state.OneNoteSectionId = page.SectionId;
            state.LastSyncDate = now;
            state.LastSyncedNodeUpdateDate = node.UpdateDate;
            state.LastSyncedOneNoteUpdateDate = page.LastModifiedDateTime;
            state.Status = "Ok";
            state.Message = "First synchronization only saved the baseline dates. Run synchronization again after changing OneNote or the Node.";
            await knowledgeDbContext.SaveChangesAsync();
            return CreateSyncResult(node, state, page, false, false);
        }

        bool oneNoteChanged = page.LastModifiedDateTime > lastSyncDate;
        bool nodeChanged = node.UpdateDate > lastSyncDate;

        if (oneNoteChanged && nodeChanged)
        {
            state.Status = "SynchronizationFailure";
            state.Message = "OneNote and the Node were both changed after the last synchronization. The user must decide which version wins.";
            await knowledgeDbContext.SaveChangesAsync();
            return CreateSyncResult(node, state, page, false, false);
        }

        if (oneNoteChanged)
        {
            CopyOneNotePageToNode(page, node, now);
            copiedFromOneNoteToNode = true;
        }
        else if (nodeChanged)
        {
            await UpdateOneNotePageFromNodeAsync(accessToken, node, page.PageId);
            page = await ReadOneNotePageAsync(accessToken, state.OneNotePageId);
            copiedFromNodeToOneNote = true;
        }

        state.OneNoteSectionId = page.SectionId;
        state.LastSyncDate = now;
        state.LastSyncedNodeUpdateDate = node.UpdateDate;
        state.LastSyncedOneNoteUpdateDate = page.LastModifiedDateTime;
        state.Status = "Ok";
        state.Message = copiedFromOneNoteToNode
            ? "OneNote was updated and the Node was not. OneNote was copied to the database."
            : copiedFromNodeToOneNote
                ? "The Node was updated and OneNote was not. The database was copied to OneNote."
                : "Nothing changed since the last synchronization.";

        await knowledgeDbContext.SaveChangesAsync();
        return CreateSyncResult(node, state, page, copiedFromOneNoteToNode, copiedFromNodeToOneNote);
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
        oneNoteSectionId NVARCHAR(200) NOT NULL,
        lastSyncDate DATETIME2 NULL,
        lastSyncedNodeUpdateDate DATETIME2 NULL,
        lastSyncedOneNoteUpdateDate DATETIME2 NULL,
        status NVARCHAR(50) NOT NULL,
        message NVARCHAR(1000) NOT NULL,
        fileName NVARCHAR(500) NOT NULL
    );

    CREATE UNIQUE INDEX IX_OneNoteSyncState_NodeId ON dbo.OneNoteSyncState(nodeId);
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
            Status = "Ok",
            Message = "Synchronization state created from Node.ExternalId."
        };

        knowledgeDbContext.OneNoteSyncStates.Add(state);
        await knowledgeDbContext.SaveChangesAsync();

        return state;
    }

    // Reads one OneNote page and its HTML content from Microsoft Graph.
    private async Task<OneNotePageInfo> ReadOneNotePageAsync(string accessToken, string pageId)
    {
        string safePageId = Uri.EscapeDataString(pageId);
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages/{safePageId}?$select=id,title,lastModifiedDateTime,links,parentSection&$expand=parentSection";

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
            LastModifiedDateTime = GetJsonDate(root, "lastModifiedDateTime"),
            WebUrl = ReadWebUrl(root),
            SectionId = ReadNestedString(root, "parentSection", "id"),
            SectionName = ReadNestedString(root, "parentSection", "displayName")
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

    // Copies the OneNote values into the database Node.
    private static void CopyOneNotePageToNode(OneNotePageInfo page, Node node, DateTime updateDate)
    {
        node.Reference = LimitText(page.Title, 1000);
        node.Description = LimitText(page.TextContent, 8000);
        node.Par2 = LimitText(page.SectionName, 200);
        node.Link = LimitText(page.WebUrl, 500);
        node.ExternalId = LimitText(page.PageId, 200);
        node.UpdateDate = updateDate;
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

    // Stores the last file name attached through this API.
    private async Task SaveAttachedFileNameAsync(string pageId, string fileName)
    {
        await CreateSyncTableIfMissingAsync();
        OneNoteSyncState? state = await knowledgeDbContext.OneNoteSyncStates.FirstOrDefaultAsync(item => item.OneNotePageId == pageId);

        if (state is not null)
        {
            state.FileName = fileName;
            state.Message = "File associated with the OneNote page.";
            await knowledgeDbContext.SaveChangesAsync();
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
        bool copiedFromNodeToOneNote)
    {
        return new OneNoteSyncResultDto
        {
            NodeId = node.Id,
            OneNotePageId = state.OneNotePageId,
            Status = state.Status,
            Message = state.Message,
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

    private class OneNotePageInfo
    {
        public string PageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public DateTime LastModifiedDateTime { get; set; }
        public string WebUrl { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
    }
}
