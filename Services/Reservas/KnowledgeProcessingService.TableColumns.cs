using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                              Source column validation
    ============================================================================
     This file checks if the source table contains the columns used by a mapping.
     Fixed text values are allowed and do not need to exist as table columns.
    ============================================================================
    */

    // Checks if the source table contains the columns required by the mapping.
    private static void ValidateTableColumns(MappingConfiguration mapping, List<string> tableColumns)
    {
        List<string> missingColumns = new List<string>();

        AddMissingColumn(missingColumns, tableColumns, mapping.IdFieldName);
        AddMissingColumn(missingColumns, tableColumns, mapping.UpdateDateFieldName);

        if (mapping.DetectionMethod.Equals("CreationDate", StringComparison.OrdinalIgnoreCase))
        {
            AddMissingColumn(missingColumns, tableColumns, mapping.CreationDateFieldName);
        }

        AddMissingNodeColumns(missingColumns, tableColumns, mapping);
        AddMissingRelationColumns(missingColumns, tableColumns, mapping);
        AddMissingParentColumns(missingColumns, tableColumns, mapping);

        if (missingColumns.Count > 0)
        {
            string missingText = string.Join(", ", missingColumns);
            string existingText = string.Join(", ", tableColumns);

            throw new InvalidOperationException(
                $"The mapping for table '{mapping.TableName}' uses columns that do not exist: {missingText}. Columns found in the table: {existingText}.");
        }
    }

    // Checks mapped columns that become Node fields.
    private static void AddMissingNodeColumns(List<string> missingColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        AddMissingMappedColumn(missingColumns, tableColumns, "reference", mapping.Mapping.Reference);
        AddMissingMappedColumn(missingColumns, tableColumns, "descricao", mapping.Mapping.Descricao);
        AddMissingMappedColumn(missingColumns, tableColumns, "idInformacao", mapping.Mapping.IdInformacao);
        AddMissingMappedColumn(missingColumns, tableColumns, "par1", mapping.Mapping.Par1);
        AddMissingMappedColumn(missingColumns, tableColumns, "par2", mapping.Mapping.Par2);
        AddMissingMappedColumn(missingColumns, tableColumns, "par3", mapping.Mapping.Par3);
        AddMissingMappedColumn(missingColumns, tableColumns, "par4", mapping.Mapping.Par4);
        AddMissingMappedColumn(missingColumns, tableColumns, "par5", mapping.Mapping.Par5);
        AddMissingMappedColumn(missingColumns, tableColumns, "par6", mapping.Mapping.Par6);
        AddMissingMappedColumn(missingColumns, tableColumns, "par7", mapping.Mapping.Par7);
        AddMissingMappedColumn(missingColumns, tableColumns, "link", mapping.Mapping.Link);
        AddMissingMappedColumn(missingColumns, tableColumns, "externalId", mapping.Mapping.ExternalId);
        AddMissingMappedColumn(missingColumns, tableColumns, "security", mapping.Mapping.Security);
        AddMissingMappedColumn(missingColumns, tableColumns, "updateUser", mapping.Mapping.UpdateUser);
        AddMissingMappedColumn(missingColumns, tableColumns, "descriptionType", mapping.Mapping.DescriptionType);
        AddMissingMappedColumn(missingColumns, tableColumns, "context.par1", mapping.Mapping.ContextPar1);
        AddMissingMappedColumn(missingColumns, tableColumns, "context.descriptionType", mapping.Mapping.ContextDescriptionType);
        AddMissingMappedColumn(missingColumns, tableColumns, "parentType", mapping.Mapping.ParentType);
    }

    // Checks mapped relation columns.
    private static void AddMissingRelationColumns(List<string> missingColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            AddMissingMappedColumn(missingColumns, tableColumns, "relations.typeId", relation.TypeId);
            AddMissingMappedColumn(missingColumns, tableColumns, "relations.targetId", relation.TargetId);
            AddMissingMappedColumn(missingColumns, tableColumns, "relations.targetType", relation.TargetType);
        }
    }

    // Checks mapped parent columns.
    private static void AddMissingParentColumns(List<string> missingColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (string parent in mapping.Mapping.Parent)
        {
            AddMissingMappedColumn(missingColumns, tableColumns, "parent", parent);
        }
    }

    // Adds one missing mapped column to the error list, unless the value is fixed text.
    private static void AddMissingMappedColumn(List<string> missingColumns, List<string> tableColumns, string fieldName, string possibleColumn)
    {
        // Empty mapping fields are allowed.
        if (string.IsNullOrWhiteSpace(possibleColumn))
        {
            return;
        }

        if (IsFixedValueField(fieldName))
        {
            return;
        }

        if (LooksLikeFixedValue(possibleColumn))
        {
            // Fixed values are allowed and do not need to exist as table columns.
            return;
        }

        if (ColumnExists(tableColumns, possibleColumn))
        {
            return;
        }

        missingColumns.Add($"{fieldName} -> {possibleColumn}");
    }

    // Adds one missing normal column to the error list.
    private static void AddMissingColumn(List<string> missingColumns, List<string> tableColumns, string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return;
        }

        if (!ColumnExists(tableColumns, columnName))
        {
            missingColumns.Add(columnName);
        }
    }

    // Returns true when the column list contains the requested column name.
    private static bool ColumnExists(List<string> tableColumns, string columnName)
    {
        foreach (string column in tableColumns)
        {
            if (column.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Returns true for fields that usually contain fixed text instead of source column names.
    private static bool IsFixedValueField(string fieldName)
    {
        return fieldName.Equals("tipo", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("tipoE", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("security", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("updateUser", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("descriptionType", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("context.par1", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("context.descriptionType", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("parentType", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("relations.typeId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("relations.targetType", StringComparison.OrdinalIgnoreCase);
    }

    // Tries to detect fixed text values that are clearly not simple column names.
    private static bool LooksLikeFixedValue(string value)
    {
        return value.Contains(':', StringComparison.Ordinal)
            || value.Contains(' ', StringComparison.Ordinal);
    }
}
