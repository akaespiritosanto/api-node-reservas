using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                  Source reading
    ============================================================================
     This part reads the source database. The table and column names come from
     the mapping file, so the service uses DbCommand instead of a fixed DbSet.
    ============================================================================
    */
    private async Task<List<Dictionary<string, object?>>> ReadRowsToProcessAsync(MappingConfiguration mapping, int limit)
    {
        List<Dictionary<string, object?>> rows = new List<Dictionary<string, object?>>();
        DbConnection connection = reservasDbContext.Database.GetDbConnection();

        try
        {
            await connection.OpenAsync();

            List<string> tableColumns = await ReadTableColumnsAsync(connection, mapping);
            ValidateTableColumns(mapping, tableColumns);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = BuildSql(mapping, tableColumns);

            AddParameter(command, "@limit", limit);
            AddParameter(command, "@lastId", mapping.LastProcessedId);
            AddParameter(command, "@lastDate", mapping.LastSuccessfulProcessingDate ?? new DateTime(1900, 1, 1));

            await using DbDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Dictionary<string, object?> row = new(StringComparer.OrdinalIgnoreCase);

                for (int index = 0; index < reader.FieldCount; index++)
                {
                    object? value = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    row[reader.GetName(index)] = value;
                }

                rows.Add(row);
            }
        }
        catch (DbException exception)
        {
            throw new InvalidOperationException(
                $"Erro ao ler a tabela '{mapping.TableName}'. Confirma se o nome da tabela e os campos do mapeamento existem na base de dados. Detalhe: {exception.Message}",
                exception);
        }
        finally
        {
            await connection.CloseAsync();
        }

        return rows;
    }

    private static async Task<List<string>> ReadTableColumnsAsync(DbConnection connection, MappingConfiguration mapping)
    {
        string table = EscapeSqlName(mapping.TableName);
        List<string> columns = new List<string>();

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT TOP (0) * FROM {table}";

        await using DbDataReader reader = await command.ExecuteReaderAsync();

        for (int index = 0; index < reader.FieldCount; index++)
        {
            columns.Add(reader.GetName(index));
        }

        return columns;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
