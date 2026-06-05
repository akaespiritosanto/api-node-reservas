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
    // Converts one source database row into the DTO used by the saving methods.
    private static KnowledgeRecordDto ConvertRowToKnowledgeRecord(MappingConfiguration mapping, Dictionary<string, object?> row)
    {
        // Contexts are extra values that will become rows in the Context table.
        List<string> contexts = new List<string>();

        foreach (string context in mapping.Mapping.Contexts)
        {
            contexts.Add(GetMappedValue(row, context));
        }

        // Parent values are optional relation targets.
        List<string> parents = new List<string>();

        foreach (string parent in mapping.Mapping.Parent)
        {
            parents.Add(GetMappedValue(row, parent));
        }

        // Relations are converted before saving so the saving code has one simple format.
        List<KnowledgeRelationDto> relations = new List<KnowledgeRelationDto>();

        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            KnowledgeRelationDto relationDto = new KnowledgeRelationDto
            {
                TypeId = GetMappedValue(row, relation.TypeId),
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
            Link = GetMappedValue(row, mapping.Mapping.Link),
            ExternalId = GetMappedValue(row, mapping.Mapping.ExternalId),
            Security = GetMappedValue(row, mapping.Mapping.Security),
            UpdateUser = GetMappedValue(row, mapping.Mapping.UpdateUser),
            DescriptionType = GetMappedValue(row, mapping.Mapping.DescriptionType),
            ContextPar1 = GetMappedValue(row, mapping.Mapping.ContextPar1),
            ContextDescriptionType = GetMappedValue(row, mapping.Mapping.ContextDescriptionType),
            ParentType = GetMappedValue(row, mapping.Mapping.ParentType),
            Contexts = contexts,
            Parent = parents,
            Relations = relations
        };
    }

    // Gets a value from the source row, or returns the mapping value as fixed text.
    private static string GetMappedValue(Dictionary<string, object?> row, string mappingValue)
    {
        // Empty mapping fields stay empty.
        if (string.IsNullOrWhiteSpace(mappingValue))
        {
            return string.Empty;
        }

        object? columnValue = GetValue(row, mappingValue);

        // If the value is not a column name, it is treated as a fixed value.
        // Example: "Reserva" is a fixed value for Tipo.
        if (columnValue is null)
        {
            return mappingValue;
        }

        return Convert.ToString(columnValue) ?? string.Empty;
    }

    // Tries to get one column value from the row dictionary.
    private static object? GetValue(Dictionary<string, object?> row, string columnName)
    {
        if (row.TryGetValue(columnName, out object? value))
        {
            return value;
        }

        return null;
    }

    // Converts text to int and returns 0 when the text is not a valid number.
    private static int ToInt(string value)
    {
        // Bad or empty numbers become 0 instead of throwing an exception.
        bool converted = int.TryParse(value, out int result);

        if (converted)
        {
            return result;
        }

        return 0;
    }

    // Cuts text so it fits inside the SQL varchar column size.
    private static string LimitText(string text, int maxLength)
    {
        // SQL varchar columns have maximum sizes, so text is cut before saving.
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
