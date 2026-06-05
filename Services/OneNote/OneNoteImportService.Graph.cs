using System.Net.Http.Headers;
using System.Text.Json;

namespace api_node_reservas.Services;

public partial class OneNoteImportService
{
    /*
    ============================================================================
                              Microsoft Graph reading
    ============================================================================
     This file contains only the calls to Microsoft Graph. It reads the signed-in
     user, lists OneNote pages and reads the HTML body for each page.
    ============================================================================
    */

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
            OneNotePageData page = CreatePageDataFromJson(pageJson, userId);

            page.ContentHtml = await ReadPageContentAsync(accessToken, page.GraphPageId);
            page.ContentText = ConvertHtmlToText(page.ContentHtml);
            pages.Add(page);
        }

        return pages;
    }

    // Creates the in-memory page object from the JSON returned by Microsoft Graph.
    private static OneNotePageData CreatePageDataFromJson(JsonElement pageJson, string userId)
    {
        return new OneNotePageData
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
}
