namespace api_node_reservas.Services;

public partial class OneNoteImportService
{
    /*
    ============================================================================
                                Imported page data
    ============================================================================
     This small class stores one OneNote page while the import is running.
     It is private because only OneNoteImportService needs it.
    ============================================================================
    */
    private class OneNotePageData
    {
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
    }
}
