using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

public partial class OneNoteImportService
{
    /*
    ============================================================================
                                OneNote JSON helpers
    ============================================================================
     Microsoft Graph returns JSON and OneNote content as HTML. These helper
     methods keep that parsing code away from the main import flow.
    ============================================================================
    */

    // Converts OneNote HTML into plain text that can be saved as a Node description.
    private static string ConvertHtmlToText(string html)
    {
        string withoutScripts = Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withSpaces = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withSpaces);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    // Reads the browser URL for a OneNote page.
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

    // Reads a displayName from a nested JSON object.
    private static string ReadNestedDisplayName(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement nested))
        {
            return string.Empty;
        }

        return GetJsonString(nested, "displayName");
    }

    // Reads an id from a nested JSON object.
    private static string ReadNestedId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement nested))
        {
            return string.Empty;
        }

        return GetJsonString(nested, "id");
    }

    // Reads a string property safely.
    private static string GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    // Reads a date property safely.
    private static DateTime GetJsonDate(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetDateTime(out DateTime value))
        {
            return value;
        }

        return DateTime.UtcNow;
    }
}
