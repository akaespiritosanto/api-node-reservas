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
    private static string BuildSql(MappingConfiguration mapping, List<string> tableColumns)
    {
        string table = EscapeSqlName(mapping.TableName);
        string idField = EscapeSqlName(mapping.IdFieldName);
        string creationField = EscapeSqlName(mapping.CreationDateFieldName);
        string updateField = EscapeSqlName(mapping.UpdateDateFieldName);
        List<string> selectedColumns = GetColumnsUsedByMapping(mapping, tableColumns);

        if (selectedColumns.Count == 0)
        {
            throw new InvalidOperationException($"O mapeamento da tabela '{mapping.TableName}' nao tem nenhuma coluna valida para selecionar.");
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
            whereClause = $"{creationField} > @lastDate OR {updateField} > @lastDate";
        }
        else
        {
            whereClause = $"{idField} > @lastId OR {updateField} > @lastDate";
        }

        return $"SELECT TOP (@limit) {selectClause} FROM {table} WHERE {whereClause} ORDER BY {idField}";
    }

    private static string EscapeSqlName(string name)
    {
        if (!SafeSqlName.IsMatch(name))
        {
            throw new InvalidOperationException($"Nome SQL invalido: {name}");
        }

        return $"[{name}]";
    }
}
