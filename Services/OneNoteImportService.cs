using api_node_reservas.Data;
using api_node_reservas.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

/*
================================================================================
                              OneNote import
================================================================================
 This service reads OneNote pages from Microsoft Graph and saves them into a
 simple source table named OneNotePageImport.

 After this import step, the normal mapping processor can read OneNotePageImport
 and save the information into the knowledge database as Node, Context and Arc.
================================================================================
*/
public class OneNoteImportService
{
    private readonly ReservasDbContext reservasDbContext;
    private readonly MicrosoftGraphAuthService authService;
    private readonly OneNoteTokenStore tokenStore;
    private readonly HttpClient httpClient;

    public OneNoteImportService(
        ReservasDbContext reservasDbContext,
        MicrosoftGraphAuthService authService,
        OneNoteTokenStore tokenStore,
        HttpClient httpClient)
    {
        this.reservasDbContext = reservasDbContext;
        this.authService = authService;
        this.tokenStore = tokenStore;
        this.httpClient = httpClient;
    }

    public async Task<OneNoteImportResultDto> ImportAsync(OneNoteImportRequestDto request)
    {
        string accessToken = await GetAccessTokenForImportAsync(request);

        await CreateImportTableIfMissingAsync();

        string userId = await ReadCurrentUserIdAsync(accessToken);
        List<OneNotePageData> pages = await ReadPagesAsync(accessToken, userId, request.Limit);

        int savedPages = 0;

        foreach (OneNotePageData page in pages)
        {
            await SavePageAsync(page);
            savedPages++;
        }

        return new OneNoteImportResultDto
        {
            PagesRead = pages.Count,
            PagesSaved = savedPages
        };
    }

    // Gets a token from the request, from an authorization code, or from memory.
    private async Task<string> GetAccessTokenForImportAsync(OneNoteImportRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return request.AccessToken;
        }

        if (!string.IsNullOrWhiteSpace(request.AuthorizationCode))
        {
            OneNoteTokenDto token = await authService.ExchangeCodeForTokenAsync(request.AuthorizationCode, request.RedirectUri);
            tokenStore.SaveToken(token.AccessToken, token.ExpiresIn);
            return token.AccessToken;
        }

        string storedToken = tokenStore.GetAccessToken();

        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            return storedToken;
        }

        throw new InvalidOperationException("Login with Microsoft first, or send an AccessToken or AuthorizationCode.");
    }

    // Reads the signed-in Microsoft user so imported pages can record who owns them.
    private async Task<string> ReadCurrentUserIdAsync(string accessToken)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=id,userPrincipalName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not read the Microsoft user. Detail: {responseText}");
        }

        using JsonDocument document = JsonDocument.Parse(responseText);
        JsonElement root = document.RootElement;
        string id = GetJsonString(root, "id");

        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return GetJsonString(root, "userPrincipalName");
    }

    // Reads the list of OneNote pages, then reads the HTML content of each page.
    private async Task<List<OneNotePageData>> ReadPagesAsync(string accessToken, string userId, int limit)
    {
        List<OneNotePageData> pages = new List<OneNotePageData>();
        string fields = "id,title,createdDateTime,lastModifiedDateTime,links,parentSection,parentNotebook";
        string url = $"https://graph.microsoft.com/v1.0/me/onenote/pages?$top={limit}&$select={fields}";

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not read OneNote pages. Detail: {responseText}");
        }

        using JsonDocument document = JsonDocument.Parse(responseText);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("value", out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            return pages;
        }

        foreach (JsonElement pageJson in value.EnumerateArray())
        {
            OneNotePageData page = new OneNotePageData
            {
                GraphPageId = GetJsonString(pageJson, "id"),
                UserId = userId,
                PageTitle = GetJsonString(pageJson, "title"),
                CreatedDateTime = GetJsonDate(pageJson, "createdDateTime"),
                LastModifiedDateTime = GetJsonDate(pageJson, "lastModifiedDateTime"),
                WebUrl = ReadWebUrl(pageJson),
                SectionName = ReadNestedDisplayName(pageJson, "parentSection"),
                NotebookName = ReadNestedDisplayName(pageJson, "parentNotebook")
            };

            page.ContentHtml = await ReadPageContentAsync(accessToken, page.GraphPageId);
            page.ContentText = ConvertHtmlToText(page.ContentHtml);
            pages.Add(page);
        }

        return pages;
    }

    // Reads the HTML body of one OneNote page.
    private async Task<string> ReadPageContentAsync(string accessToken, string graphPageId)
    {
        string pageId = Uri.EscapeDataString(graphPageId);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/onenote/pages/{pageId}/content");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not read OneNote page content. Detail: {responseText}");
        }

        return responseText;
    }

    // Creates the staging table automatically if the database does not have it yet.
    private async Task CreateImportTableIfMissingAsync()
    {
        string sql = @"
IF OBJECT_ID('dbo.OneNotePageImport', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OneNotePageImport
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        graphPageId NVARCHAR(200) NOT NULL,
        userId NVARCHAR(200) NOT NULL,
        notebookName NVARCHAR(500) NOT NULL,
        sectionName NVARCHAR(500) NOT NULL,
        pageTitle NVARCHAR(1000) NOT NULL,
        contentText NVARCHAR(MAX) NOT NULL,
        contentHtml NVARCHAR(MAX) NOT NULL,
        createdDateTime DATETIME2 NOT NULL,
        lastModifiedDateTime DATETIME2 NOT NULL,
        webUrl NVARCHAR(1000) NOT NULL,
        importedAt DATETIME2 NOT NULL
    );

    CREATE UNIQUE INDEX IX_OneNotePageImport_GraphPageId ON dbo.OneNotePageImport(graphPageId);
END";

        await reservasDbContext.Database.ExecuteSqlRawAsync(sql);
    }

    // Inserts a new page or updates the existing row for the same Microsoft page id.
    private async Task SavePageAsync(OneNotePageData page)
    {
        DbConnection connection = reservasDbContext.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;

        if (closeConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT id FROM dbo.OneNotePageImport WHERE graphPageId = @graphPageId";
            AddParameter(existsCommand, "@graphPageId", page.GraphPageId);

            object? existingId = await existsCommand.ExecuteScalarAsync();

            if (existingId is null)
            {
                await InsertPageAsync(connection, page);
            }
            else
            {
                await UpdatePageAsync(connection, page);
            }
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Adds one new row to OneNotePageImport.
    private static async Task InsertPageAsync(DbConnection connection, OneNotePageData page)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.OneNotePageImport
(graphPageId, userId, notebookName, sectionName, pageTitle, contentText, contentHtml, createdDateTime, lastModifiedDateTime, webUrl, importedAt)
VALUES
(@graphPageId, @userId, @notebookName, @sectionName, @pageTitle, @contentText, @contentHtml, @createdDateTime, @lastModifiedDateTime, @webUrl, @importedAt)";

        AddPageParameters(command, page);
        await command.ExecuteNonQueryAsync();
    }

    // Updates the row when this OneNote page was imported before.
    private static async Task UpdatePageAsync(DbConnection connection, OneNotePageData page)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.OneNotePageImport
SET userId = @userId,
    notebookName = @notebookName,
    sectionName = @sectionName,
    pageTitle = @pageTitle,
    contentText = @contentText,
    contentHtml = @contentHtml,
    createdDateTime = @createdDateTime,
    lastModifiedDateTime = @lastModifiedDateTime,
    webUrl = @webUrl,
    importedAt = @importedAt
WHERE graphPageId = @graphPageId";

        AddPageParameters(command, page);
        await command.ExecuteNonQueryAsync();
    }

    // Adds all page values to a SQL command as parameters.
    private static void AddPageParameters(DbCommand command, OneNotePageData page)
    {
        AddParameter(command, "@graphPageId", page.GraphPageId);
        AddParameter(command, "@userId", page.UserId);
        AddParameter(command, "@notebookName", page.NotebookName);
        AddParameter(command, "@sectionName", page.SectionName);
        AddParameter(command, "@pageTitle", page.PageTitle);
        AddParameter(command, "@contentText", page.ContentText);
        AddParameter(command, "@contentHtml", page.ContentHtml);
        AddParameter(command, "@createdDateTime", page.CreatedDateTime);
        AddParameter(command, "@lastModifiedDateTime", page.LastModifiedDateTime);
        AddParameter(command, "@webUrl", page.WebUrl);
        AddParameter(command, "@importedAt", DateTime.UtcNow);
    }

    // Adds one value to a SQL command without writing the value directly into SQL text.
    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    // Converts OneNote HTML into plain text that can be saved as a Node description.
    private static string ConvertHtmlToText(string html)
    {
        string withoutScripts = Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withSpaces = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withSpaces);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
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

    private static string ReadNestedDisplayName(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement nested))
        {
            return string.Empty;
        }

        return GetJsonString(nested, "displayName");
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

    private class OneNotePageData
    {
        public string GraphPageId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NotebookName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string ContentText { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public string WebUrl { get; set; } = string.Empty;
    }
}
