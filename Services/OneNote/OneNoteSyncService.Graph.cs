using System.Net.Http.Headers;
using System.Text.Json;
using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
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

    // After changing OneNote, Microsoft Graph can need a moment before reads
    // show the new title/body. This method waits a little and verifies that the
    // page really contains the Node values before the sync reports success.
    private async Task<OneNotePageInfo> ReadOneNotePageAfterNodeUpdateAsync(
        string accessToken,
        Node node,
        OneNotePageInfo oldPage)
    {
        OneNotePageInfo updatedPage = oldPage;

        for (int attempt = 1; attempt <= 5; attempt++)
        {
            await Task.Delay(1000);
            updatedPage = await ReadOneNotePageAsync(accessToken, oldPage.PageId);

            if (OneNotePageMatchesNode(updatedPage, node))
            {
                return updatedPage;
            }
        }

        throw new InvalidOperationException(
            "The API sent the change to OneNote, but OneNote did not return the new Node values when it was checked again. Try the sync again in a few seconds, and confirm that the signed-in Microsoft account can edit this page.");
    }
}
