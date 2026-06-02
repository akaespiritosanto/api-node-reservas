using System.ComponentModel.DataAnnotations;

namespace api_node_reservas.Dtos;

// Sent when importing OneNote pages. In the normal flow, only Limit is needed
// because the token was already saved by the callback endpoint.
public class OneNoteImportRequestDto
{
    public string AuthorizationCode { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
