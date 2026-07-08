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
        AddColumnIfExists(selectedColumns, tableColumns, "lastModifiedDateTime");
        AddColumnIfExists(selectedColumns, tableColumns, "importedAt");

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
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Link);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.ExternalId);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.Security);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.UpdateUser);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.DescriptionType);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.ContextPar1);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.ContextDescriptionType);
        AddColumnIfExists(selectedColumns, tableColumns, mapping.Mapping.ParentType);
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
        foreach (KbParentMapping parent in mapping.Mapping.Parent)
        {
            AddColumnIfExists(selectedColumns, tableColumns, parent.FieldName);
            AddColumnIfExists(selectedColumns, tableColumns, parent.FieldId);
            AddColumnIfExists(selectedColumns, tableColumns, parent.GroupBy);
            AddColumnIfExists(selectedColumns, tableColumns, parent.GroupById);
        }
    }

    // Adds mapped custom relation columns used for relation targets.
    private static void AddRelationColumnsIfTheyExist(List<string> selectedColumns, List<string> tableColumns, MappingConfiguration mapping)
    {
        foreach (KbRelationMapping relation in mapping.Mapping.Relations)
        {
            AddColumnIfExists(selectedColumns, tableColumns, relation.TypeId);
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

        // Try to find the real column name in the source table. The mapping
        // may use different casing or different text; FindRealColumnName
        // searches case-insensitively and returns the actual table column
        // name when found.
        string? realColumnName = FindRealColumnName(tableColumns, possibleColumn);

        // If the source table does not have this column, skip it.
        if (realColumnName is null)
        {
            return;
        }

        // Avoid adding the same column twice.
        if (ColumnExists(selectedColumns, realColumnName))
        {
            return;
        }

        selectedColumns.Add(realColumnName);
    }

    // Finds the real table column name, keeping the database casing.
    private static string? FindRealColumnName(List<string> tableColumns, string possibleColumn)
    {
        // The source table column names are returned from the database with
        // their real casing. We want to match mapping names in a case-
        // insensitive way but keep the exact database name when adding
        // it to the SELECT list.
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
