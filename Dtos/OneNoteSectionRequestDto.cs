namespace api_node_reservas.Dtos;

public class OneNoteCreateSectionRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string NotebookId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class OneNoteRenameSectionRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class OneNoteSectionResultDto
{
    public string SectionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
