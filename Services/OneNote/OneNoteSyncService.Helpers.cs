using api_node_reservas.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
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
        string bodyHtml = GetHtmlBody(html);
        string withoutScripts = Regex.Replace(bodyHtml, "<script.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withSpaces = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withSpaces);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    // OneNote returns a complete HTML document. For note content we only want
    // the body, otherwise the page title can be mixed into the text comparison.
    private static string GetHtmlBody(string html)
    {
        Match bodyMatch = Regex.Match(html, "<body[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (bodyMatch.Success)
        {
            return bodyMatch.Groups[1].Value;
        }

        return Regex.Replace(html, "<head.*?</head>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    private static string CreateHtmlFromNodeDescription(Node node)
    {
        return $"<div>{WebUtility.HtmlEncode(node.Description)}</div>";
    }

    private static bool OneNotePageMatchesNode(OneNotePageInfo page, Node node)
    {
        bool sameTitle = SameText(page.Title, node.Reference);
        string pageText = NormalizeTextForComparison(page.TextContent);
        string nodeText = NormalizeTextForComparison(node.Description);
        bool sameBody = SameText(pageText, nodeText) || pageText.Contains(nodeText, StringComparison.Ordinal);

        return sameTitle && sameBody;
    }

    private static string NormalizeTextForComparison(string? text)
    {
        string safeText = text ?? string.Empty;
        return Regex.Replace(safeText, @"\s+", " ").Trim();
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
    // OneNotePageImport. This is kept for debugging and future options, but the
    // sync decision currently trusts Microsoft's lastModifiedDateTime for OneNote.
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
}
