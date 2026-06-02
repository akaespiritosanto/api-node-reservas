namespace api_node_reservas.Dtos;

// Returned after OneNote pages are imported into the staging table.
public class OneNoteImportResultDto
{
    public int PagesRead { get; set; }
    public int PagesSaved { get; set; }
}
