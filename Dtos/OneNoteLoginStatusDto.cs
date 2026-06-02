namespace api_node_reservas.Dtos;

// Shows whether the API has a temporary Microsoft access token in memory.
public class OneNoteLoginStatusDto
{
    public bool HasAccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
