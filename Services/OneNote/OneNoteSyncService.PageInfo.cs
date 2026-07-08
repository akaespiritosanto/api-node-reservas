namespace api_node_reservas.Services;

public partial class OneNoteSyncService
{
    private class OneNotePageInfo
    {
        public string PageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public string WebUrl { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string NotebookId { get; set; } = string.Empty;
        public string NotebookName { get; set; } = string.Empty;
    }
}
