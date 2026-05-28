using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                               Mapping validation
    ============================================================================
     This part checks if the mapping is safe and if the columns mentioned in the
     mapping really exist in the source table.
    ============================================================================
    */
    // Checks if the main SQL names in the mapping are safe to use.
    private static void ValidateMapping(MappingConfiguration mapping)
    {
        EscapeSqlName(mapping.TableName);
        EscapeSqlName(mapping.IdFieldName);
        EscapeSqlName(mapping.CreationDateFieldName);
        EscapeSqlName(mapping.UpdateDateFieldName);
    }

    // Builds the list of source table columns that the SQL SELECT must read.
    private static List<string> GetColumnsUsedByMapping(MappingConfiguration mapping, List<string> tableColumns)
    {
        List<string> selectedColumns = new List<string>();

        // These columns are needed to find and order the rows that will be processed.
        AddColumnIfExists(selectedColumns, tableColumns, mapping.IdFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.CreationDateFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.UpdateDateFieldName);

        // These columns are the values that become Node fields.
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Tipo);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.TipoE);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Reference);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Descricao);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.IdInformacao);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par1);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par2);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par3);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par4);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par5);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par6);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Par7);

        foreach (string context in mapping.Mapping.Contexts)
        {
            // Context values become rows in the Context table.
            AddColumnIfExists(selectedColumns, tableColumns, context);
        }

        foreach (string parent in mapping.Mapping.Parent)
        {
            // Parent values are optional relation targets.
            AddColumnIfExists(selectedColumns, tableColumns, parent);
        }

        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            // Relation mappings need the target id and, sometimes, the target type.
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetId);
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetType);
        }

        return selectedColumns;
    }

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

        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            AddMissingMappedColumn(missingColumns, tableColumns, "relations.targetId", relation.TargetId);
            AddMissingMappedColumn(missingColumns, tableColumns, "relations.targetType", relation.TargetType);
        }

        foreach (string parent in mapping.Mapping.Parent)
        {
            AddMissingMappedColumn(missingColumns, tableColumns, "parent", parent);
        }

        if (missingColumns.Count > 0)
        {
            string missingText = string.Join(", ", missingColumns);
            string existingText = string.Join(", ", tableColumns);

            throw new InvalidOperationException(
                $"The mapping for table '{mapping.TableName}' uses columns that do not exist: {missingText}. Columns found in the table: {existingText}.");
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
            || fieldName.Equals("relations.targetType", StringComparison.OrdinalIgnoreCase);
    }

    // Tries to detect fixed text values that are clearly not simple column names.
    private static bool LooksLikeFixedValue(string value)
    {
        return value.Contains(':', StringComparison.Ordinal)
            || value.Contains(' ', StringComparison.Ordinal);
    }

    // Adds a source column to the SELECT list only if it exists and was not already added.
    private static void AddColumnIfExists(List<string> selectedColumns, List<string> tableColumns, string possibleColumn)
    {
        if (string.IsNullOrWhiteSpace(possibleColumn))
        {
            return;
        }

        string? realColumnName = null;

        foreach (string tableColumn in tableColumns)
        {
            if (tableColumn.Equals(possibleColumn, StringComparison.OrdinalIgnoreCase))
            {
                realColumnName = tableColumn;
                break;
            }
        }

        if (realColumnName is null)
        {
            return;
        }

        if (ColumnExists(selectedColumns, realColumnName))
        {
            return;
        }

        selectedColumns.Add(realColumnName);
    }
}
