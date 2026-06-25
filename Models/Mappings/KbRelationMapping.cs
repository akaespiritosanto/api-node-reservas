namespace api_node_reservas.Models;

public class KbRelationMapping
{
    // Generic field name in the source table that holds relation info
    public string Field { get; set; } = string.Empty;

    // High-level relation descriptor (optional)
    public string RelationType { get; set; } = string.Empty;
    public int RelationTypeId { get; set; }

    // These properties are used by the processing code to build Arcs
    // TypeId: numeric id for the arc type in the knowledge DB. Stored as
    // string in the mapping classes because mappings may use a fixed text
    // like "0" or a source column name. Conversion to int happens later during saving.
    public string TypeId { get; set; } = string.Empty;

    // Type: textual name for the arc type
    public string Type { get; set; } = string.Empty;

    // TargetId: field name (or literal) that identifies the target node
    public string TargetId { get; set; } = string.Empty;

    // TargetType: textual name for the target node type
    public string TargetType { get; set; } = string.Empty;

    // SourceId: field name that identifies the source node (optional)
    public string SourceId { get; set; } = string.Empty;
}
