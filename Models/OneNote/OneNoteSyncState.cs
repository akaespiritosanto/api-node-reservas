namespace api_node_reservas.Models;

/*
================================================================================
                              OneNoteSyncState
================================================================================
 This model stores the dates used by the bidirectional synchronization.
 It is kept in its own table so the original Node table does not need new
 columns just to remember OneNote synchronization information.
================================================================================
*/
public class OneNoteSyncState
{
    public int Id { get; set; }
    public int NodeId { get; set; }
    public string OneNotePageId { get; set; } = string.Empty;
    public string OneNoteSectionId { get; set; } = string.Empty;
    public DateTime? LastSyncDate { get; set; }
    public DateTime? LastSyncedNodeUpdateDate { get; set; }
    public DateTime? LastSyncedOneNoteUpdateDate { get; set; }
    public string Status { get; set; } = "Ok";
    public string Message { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
