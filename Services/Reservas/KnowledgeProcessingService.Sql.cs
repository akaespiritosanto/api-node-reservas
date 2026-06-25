using api_node_reservas.Models;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    private static readonly Regex SafeSqlName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /*
    ============================================================================
                                  SQL building
    ============================================================================
     This part creates the SELECT statement. Table and column names are checked
     before being used, and values are sent as parameters.
    ============================================================================
    */
    // Builds the SQL SELECT command that reads only new or updated rows.
    private static string BuildSql(MappingConfiguration mapping, List<string> tableColumns)
    {
        // SQL names are escaped before they are placed inside the query text.
        string table = EscapeSqlName(mapping.TableName);
        string idField = EscapeSqlName(mapping.IdFieldName);

        // Creation and update fields may be empty in some mappings (beginner defaults).
        // Only escape them if they are provided; otherwise treat as absent.
        string? creationField = string.IsNullOrWhiteSpace(mapping.CreationDateFieldName)
            ? null
            : EscapeSqlName(mapping.CreationDateFieldName);

        string? updateField = string.IsNullOrWhiteSpace(mapping.UpdateDateFieldName)
            ? null
            : EscapeSqlName(mapping.UpdateDateFieldName);
        List<string> selectedColumns = GetColumnsUsedByMapping(mapping, tableColumns);

        if (selectedColumns.Count == 0)
        {
            throw new InvalidOperationException($"The mapping for table '{mapping.TableName}' has no valid columns to select.");
        }

        List<string> escapedColumns = new List<string>();

        foreach (string selectedColumn in selectedColumns)
        {
            escapedColumns.Add(EscapeSqlName(selectedColumn));
        }

        string selectClause = string.Join(", ", escapedColumns);

        string whereClause;

        if (mapping.DetectionMethod.Equals("CreationDate", StringComparison.OrdinalIgnoreCase))
        {
            // CreationDate mode uses dates to decide what changed.
            if (creationField is null && updateField is null)
            {
                throw new InvalidOperationException($"Mapping for table '{mapping.TableName}' requires at least one date field when using CreationDate detection.");
            }

            List<string> parts = new List<string>();
            if (creationField is not null) parts.Add($"{creationField} > @lastDate");
            if (updateField is not null) parts.Add($"{updateField} > @lastDate");
            whereClause = string.Join(" OR ", parts);
        }
        else
        {
            // Id mode uses the last processed id and optionally checks the update date.
            if (updateField is null)
            {
                whereClause = $"{idField} > @lastId";
            }
            else
            {
                whereClause = $"{idField} > @lastId OR {updateField} > @lastDate";
            }
        }

        return $"SELECT TOP (@limit) {selectClause} FROM {table} WHERE {whereClause} ORDER BY {idField}";
    }

    // Checks if a table or column name is safe, then wraps it in SQL brackets.
    private static string EscapeSqlName(string name)
    {
        // Only simple names are allowed, for example Reserva or data_actualizacao.
        if (!SafeSqlName.IsMatch(name))
        {
            throw new InvalidOperationException($"Invalid SQL name: {name}");
        }

        return $"[{name}]";
    }
}
