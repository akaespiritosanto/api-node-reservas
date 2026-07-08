using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
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
        string html = CreateHtmlFromNodeDescription(node);
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
}
