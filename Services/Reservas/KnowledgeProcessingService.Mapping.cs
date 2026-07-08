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
        List<KnowledgeParentDto> parentMappings = new List<KnowledgeParentDto>();

        foreach (KbParentMapping parent in mapping.Mapping.Parent)
        {
            // Always create a structured parent mapping DTO so the context tree
            // logic can create notebook/section nodes and contexts even when
            // groupBy is not provided. This keeps behavior simple and explicit.
            KnowledgeParentDto parentDto = new KnowledgeParentDto
            {
                FieldName = GetMappedValue(row, parent.FieldName),
                FieldId = GetMappedValue(row, parent.FieldId),
                ParentType = parent.ParentType,
                ParentTypeId = parent.ParentTypeId,
                GroupBy = GetMappedValue(row, parent.GroupBy),
                GroupById = GetMappedValue(row, parent.GroupById),
                GroupByType = parent.GroupByType,
                GroupByTypeId = parent.GroupByTypeId
            };

            parentMappings.Add(parentDto);

            // Preserve the original simple parent list for backwards
            // compatibility: when the mapping did not use groupBy the code
            // previously added the fieldName value to the Parent list so arcs
            // and other logic continued to work. Keep that behavior.
            if (string.IsNullOrWhiteSpace(parent.GroupBy))
            {
                parents.Add(GetMappedValue(row, parent.FieldName));
            }
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
            LastModifiedDateTime = ToNullableDate(GetValue(row, "lastModifiedDateTime")),
            ImportedAt = ToNullableDate(GetValue(row, "importedAt")),
            ContextPar1 = GetMappedValue(row, mapping.Mapping.ContextPar1),
            ContextDescriptionType = GetMappedValue(row, mapping.Mapping.ContextDescriptionType),
            ParentType = GetMappedValue(row, mapping.Mapping.ParentType),
            Contexts = contexts,
            Parent = parents,
            ParentMappings = parentMappings,
            Relations = relations
        };
    }

    // Gets a value from the source row, or returns the mapping value as fixed text.
    private static string GetMappedValue(Dictionary<string, object?> row, string mappingValue)
    {
        // If the mapping is empty or whitespace, there is nothing to map.
        if (string.IsNullOrWhiteSpace(mappingValue))
        {
            return string.Empty;
        }

        // If the source row contains a column with the mapping name then the
        // mapping refers to an actual column. We use TryGetValue so we can
        // distinguish between a missing column and a column that exists but
        // contains NULL in the database.
        if (row.TryGetValue(mappingValue, out object? columnValue))
        {
            // Column exists. If the database value was NULL, return empty string.
            if (columnValue is null)
            {
                return string.Empty;
            }

            // Convert the column value to string in a safe way.
            string? converted = Convert.ToString(columnValue);
            return converted ?? string.Empty;
        }

        // If the mappingValue is not a column name then we treat it as fixed
        // text provided in the mapping configuration. Example: "Reserva".
        return mappingValue;
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
        // TryParse will not throw. If parsing succeeds we return the number,
        // otherwise we return 0. This keeps the rest of the code simple.
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return 0;
    }

    // Converts database date values to nullable DateTime for optional fields.
    private static DateTime? ToNullableDate(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is DateTime date)
        {
            return date;
        }

        if (DateTime.TryParse(Convert.ToString(value), out DateTime parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    // Cuts text so it fits inside the SQL varchar column size.
    private static string LimitText(string text, int maxLength)
    {
        // SQL varchar columns have maximum sizes, so text is cut before saving.
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // If the text is already short enough, return it unchanged.
        if (text.Length <= maxLength)
        {
            return text;
        }

        // Otherwise cut the text to the allowed maximum length and return it.
        return text.Substring(0, maxLength);
    }
}
