using System.Text.Json;
using System.Text.Json.Serialization;

namespace api_node_reservas.Models;

[JsonConverter(typeof(KbParentMappingJsonConverter))]
public class KbParentMapping
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public int ParentTypeId { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public string GroupById { get; set; } = string.Empty;
    public string GroupByType { get; set; } = string.Empty;
    public int GroupByTypeId { get; set; }
}

// This converter supports two formats for the "Parent" entry in mapping JSON:
// 1) Simple string form used historically: "Parent": [ "id_reserva" ]
//    In this case the converter treats the string as FieldName and leaves
//    the other properties empty or zero.
// 2) Full object form used for notebook/section/note grouping:
//    {
//      "fieldName": "notebookName",
//      "fieldId": "notebookId",
//      "parentType": "notebook",
//      "parentTypeId": 3001,
//      "groupBy": "sectionName",
//      "groupById": "sectionId",
//      "groupByType": "section",
//      "groupByTypeId": 3002
//    }
// The converter detects the JSON token kind and returns a KbParentMapping
// instance populated appropriately. This keeps backward compatibility while
// enabling the richer context tree mapping.
// Allows old mappings like "Parent": ["id_reserva"] to keep working.
public class KbParentMappingJsonConverter : JsonConverter<KbParentMapping>
{
    public override KbParentMapping Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new KbParentMapping
            {
                FieldName = reader.GetString() ?? string.Empty
            };
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return new KbParentMapping
        {
            FieldName = GetString(root, "fieldName"),
            FieldId = GetString(root, "fieldId"),
            ParentType = GetString(root, "parentType"),
            ParentTypeId = GetInt(root, "parentTypeId"),
            GroupBy = GetString(root, "groupBy"),
            GroupById = GetString(root, "groupById"),
            GroupByType = GetString(root, "groupByType"),
            GroupByTypeId = GetInt(root, "groupByTypeId")
        };
    }

    public override void Write(Utf8JsonWriter writer, KbParentMapping value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("fieldName", value.FieldName);
        writer.WriteString("fieldId", value.FieldId);
        writer.WriteString("parentType", value.ParentType);
        writer.WriteNumber("parentTypeId", value.ParentTypeId);
        writer.WriteString("groupBy", value.GroupBy);
        writer.WriteString("groupById", value.GroupById);
        writer.WriteString("groupByType", value.GroupByType);
        writer.WriteNumber("groupByTypeId", value.GroupByTypeId);
        writer.WriteEndObject();
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out JsonElement property))
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int GetInt(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out JsonElement property) && property.TryGetInt32(out int value))
        {
            return value;
        }

        return 0;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property)
    {
        foreach (JsonProperty jsonProperty in root.EnumerateObject())
        {
            if (jsonProperty.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = jsonProperty.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
