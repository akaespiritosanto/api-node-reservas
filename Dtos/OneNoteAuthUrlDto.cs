namespace api_node_reservas.Dtos;

// Returned by GET /api/onenote/login-url.
public class OneNoteAuthUrlDto
{
    public string AuthorizationUrl { get; set; } = string.Empty;
}
