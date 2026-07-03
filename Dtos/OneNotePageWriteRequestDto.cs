namespace api_node_reservas.Dtos;

public class OneNoteUpdatePageRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string PageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
}

public class OneNoteAttachFileRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string PageId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Base64Content { get; set; } = string.Empty;
}

public class OneNoteWriteResultDto
{
    public string PageId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
