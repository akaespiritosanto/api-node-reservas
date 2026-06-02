namespace api_node_reservas.Models;

/*
================================================================================
                             OneNotePageImport
================================================================================
 This model represents the staging table used for OneNote imports.
 OneNote pages are saved here first, then mapped into Node, Context and Arc by
 the normal knowledge processing service.
================================================================================
*/
public class OneNotePageImport
{
    public int Id { get; set; }
    public string GraphPageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string NotebookName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public string WebUrl { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
}
