using api_node_reservas.Dtos;
using System.Text.Json;

namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
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
}
