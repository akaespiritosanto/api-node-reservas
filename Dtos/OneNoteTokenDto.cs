namespace api_node_reservas.Dtos;

// Returned after a Microsoft authorization code is exchanged for a token.
public class OneNoteTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
