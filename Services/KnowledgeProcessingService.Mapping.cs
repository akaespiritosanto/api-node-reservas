using api_node_reservas.Dtos;
using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                Row conversion
    ============================================================================
     This part converts one source database row into a simple DTO. After this
     conversion, the saving code no longer needs to know the original table name.
    ============================================================================
    */
    private static KnowledgeRecordDto ConvertRowToKnowledgeRecord(MappingConfiguration mapping, Dictionary<string, object?> row)
    {
        List<string> contexts = new();

        foreach (string context in mapping.Mapping.Contexts)
        {
            contexts.Add(GetMappedValue(row, context));
        }

        List<string> parents = new();

        foreach (string parent in mapping.Mapping.Parent)
        {
            parents.Add(GetMappedValue(row, parent));
        }

        List<KnowledgeRelationDto> relations = new();

        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            KnowledgeRelationDto relationDto = new()
            {
                Type = relation.Type,
                TargetId = GetMappedValue(row, relation.TargetId),
                TargetType = GetMappedValue(row, relation.TargetType)
            };

            relations.Add(relationDto);
        }

        return new KnowledgeRecordDto
        {
            SourceTable = mapping.TableName,
            SourceId = Convert.ToInt32(GetValue(row, mapping.IdFieldName) ?? 0),
            Tipo = GetMappedValue(row, mapping.Mapping.Tipo),
            TipoE = GetMappedValue(row, mapping.Mapping.TipoE),
            Reference = GetMappedValue(row, mapping.Mapping.Reference),
            Descricao = GetMappedValue(row, mapping.Mapping.Descricao),
            IdInformacao = GetMappedValue(row, mapping.Mapping.IdInformacao),
            Par1 = GetMappedValue(row, mapping.Mapping.Par1),
            Par2 = GetMappedValue(row, mapping.Mapping.Par2),
            Par3 = GetMappedValue(row, mapping.Mapping.Par3),
            Par4 = GetMappedValue(row, mapping.Mapping.Par4),
            Par5 = GetMappedValue(row, mapping.Mapping.Par5),
            Par6 = GetMappedValue(row, mapping.Mapping.Par6),
            Par7 = GetMappedValue(row, mapping.Mapping.Par7),
            Contexts = contexts,
            Parent = parents,
            Relations = relations
        };
    }

    private static string GetMappedValue(Dictionary<string, object?> row, string mappingValue)
    {
        if (string.IsNullOrWhiteSpace(mappingValue))
        {
            return string.Empty;
        }

        object? columnValue = GetValue(row, mappingValue);

        if (columnValue is null)
        {
            return mappingValue;
        }

        return Convert.ToString(columnValue) ?? string.Empty;
    }

    private static object? GetValue(Dictionary<string, object?> row, string columnName)
    {
        if (row.TryGetValue(columnName, out object? value))
        {
            return value;
        }

        return null;
    }

    private static int ToInt(string value)
    {
        bool converted = int.TryParse(value, out int result);

        if (converted)
        {
            return result;
        }

        return 0;
    }

    private static string LimitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength];
    }
}
