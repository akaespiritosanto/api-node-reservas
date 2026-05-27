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
    private static void ValidateMapping(MappingConfiguration mapping)
    {
        EscapeSqlName(mapping.TableName);
        EscapeSqlName(mapping.IdFieldName);
        EscapeSqlName(mapping.CreationDateFieldName);
        EscapeSqlName(mapping.UpdateDateFieldName);
    }

    private static List<string> GetColumnsUsedByMapping(MappingConfiguration mapping, List<string> tableColumns)
    {
        List<string> selectedColumns = new List<string>();

        AddColumnIfExists(selectedColumns, tableColumns, mapping.IdFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.CreationDateFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.UpdateDateFieldName);
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
            AddColumnIfExists(selectedColumns, tableColumns, context);
        }

        foreach (string parent in mapping.Mapping.Parent)
        {
            AddColumnIfExists(selectedColumns, tableColumns, parent);
        }

        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetId);
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetType);
        }

        return selectedColumns;
    }

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
                $"O mapeamento da tabela '{mapping.TableName}' usa colunas que nao existem: {missingText}. Colunas encontradas na tabela: {existingText}.");
        }
    }

    private static void AddMissingMappedColumn(List<string> missingColumns, List<string> tableColumns, string fieldName, string possibleColumn)
    {
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
            return;
        }

        if (ColumnExists(tableColumns, possibleColumn))
        {
            return;
        }

        missingColumns.Add($"{fieldName} -> {possibleColumn}");
    }

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

    private static bool IsFixedValueField(string fieldName)
    {
        return fieldName.Equals("tipo", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("tipoE", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("relations.targetType", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeFixedValue(string value)
    {
        return value.Contains(':', StringComparison.Ordinal)
            || value.Contains(' ', StringComparison.Ordinal);
    }

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
