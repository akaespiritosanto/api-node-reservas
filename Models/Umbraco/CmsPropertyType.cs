namespace api_node_reservas.Models;

// Maps the cmsPropertyType table - defines available properties for a content type
public class CmsPropertyType
{
    public int id { get; set; }
    public int dataTypeId { get; set; }
    public int contentTypeId { get; set; }
    public int? propertyTypeGroupId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? helpText { get; set; }
    public int sortOrder { get; set; }
    public bool mandatory { get; set; }
    public string? validationRegExp { get; set; }
    public string? Description { get; set; }
}
