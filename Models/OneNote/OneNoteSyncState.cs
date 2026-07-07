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
    public DateTime? LastSyncDate { get; set; }
    public DateTime? NodeUpdateDate { get; set; }
    public DateTime? OneNoteUpdateDate { get; set; }
    public string Status { get; set; } = "Ok";
}
