namespace api_node_reservas.Dtos;

public class OneNoteSyncRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
}

public class OneNoteSyncResultDto
{
    public int NodeId { get; set; }
    public string OneNotePageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool CopiedFromOneNoteToNode { get; set; }
    public bool CopiedFromNodeToOneNote { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public DateTime? NodeUpdateDate { get; set; }
    public DateTime? OneNoteUpdateDate { get; set; }
}
