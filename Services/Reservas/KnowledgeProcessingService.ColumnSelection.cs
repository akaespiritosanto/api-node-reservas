using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                               SELECT column choice
    ============================================================================
     This file builds the list of table columns that should be read from SQL.
     Only columns that exist in the source table are added to the SELECT.
    ============================================================================
    */

    // Builds the list of source table columns that the SQL SELECT must read.
    private static List<string> GetColumnsUsedByMapping(MappingConfiguration mapping, List<string> tableColumns)
    {
        List<string> selectedColumns = new List<string>();

        // These columns are needed to find and order the rows that will be processed.
        AddColumnIfExists(selectedColumns, tableColumns, mapping.IdFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.CreationDateFieldName);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.UpdateDateFieldName);

        // These columns are the values that become Node fields.
        AddNodeColumnsIfTheyExist(selectedColumns, tableColumns, mapping);
        AddContextColumnsIfTheyExist(selectedColumns, tableColumns, mapping);
        AddParentColumnsIfTheyExist(selectedColumns, tableColumns, mapping);
        AddRelationColumnsIfTheyExist(selectedColumns, tableColumns, mapping);

        return selectedColumns;
    }

    // Adds the mapped columns that become Node fields.
    private static void AddNodeColumnsIfTheyExist(List<string> selectedColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
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
    }

    // Adds the mapped columns that become Context rows.
    private static void AddContextColumnsIfTheyExist(List<string> selectedColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (string context in mapping.Mapping.Contexts)
        {
            AddColumnIfExists(selectedColumns, tableColumns, context);
        }
    }

    // Adds mapped parent columns used for relation targets.
    private static void AddParentColumnsIfTheyExist(List<string> selectedColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (string parent in mapping.Mapping.Parent)
        {
            AddColumnIfExists(selectedColumns, tableColumns, parent);
        }
    }

    // Adds mapped custom relation columns used for relation targets.
    private static void AddRelationColumnsIfTheyExist(List<string> selectedColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetId);
            AddColumnIfExists(selectedColumns, tableColumns, relation.TargetType);
        }
    }

    // Adds a source column to the SELECT list only if it exists and was not already added.
    private static void AddColumnIfExists(List<string> selectedColumns, List<string> tableColumns, string possibleColumn)
    {
        if (string.IsNullOrWhiteSpace(possibleColumn))
        {
            return;
        }

        string? realColumnName = FindRealColumnName(tableColumns, possibleColumn);

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

    // Finds the real table column name, keeping the database casing.
    private static string? FindRealColumnName(List<string> tableColumns, string possibleColumn)
    {
        foreach (string tableColumn in tableColumns)
        {
            if (tableColumn.Equals(possibleColumn, StringComparison.OrdinalIgnoreCase))
            {
                return tableColumn;
            }
        }

        return null;
    }
}
