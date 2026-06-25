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
    // Reads the changed source rows that should be processed for this mapping.
    private async Task<List<Dictionary<string, object?>>> ReadRowsToProcessAsync(MappingConfiguration mapping, int limit)
    {
        // Delegate to the overload that accepts an explicit connection.
        DbConnection connection = reservasDbContext.Database.GetDbConnection();
        return await ReadRowsToProcessAsync(mapping, limit, connection);
    }

    // Overload that reads rows using an explicit database connection. This allows
    // the same processing code to read from different source databases (e.g.
    // Reservas or Umbraco) depending on the mapping repository in use.
    private static async Task<List<Dictionary<string, object?>>> ReadRowsToProcessAsync(MappingConfiguration mapping, int limit, DbConnection connection)
    {
        List<Dictionary<string, object?>> rows = new List<Dictionary<string, object?>>();

        try
        {
            await connection.OpenAsync();

            // Read the real column names first so bad mappings fail with a clear message.
            List<string> tableColumns = await ReadTableColumnsAsync(connection, mapping);
            ValidateTableColumns(mapping, tableColumns);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = BuildSql(mapping, tableColumns);

            AddParameter(command, "@limit", limit);
            AddParameter(command, "@lastId", mapping.LastProcessedId);
            AddParameter(command, "@lastDate", mapping.LastSuccessfulProcessingDate ?? new DateTime(1900, 1, 1));

            // The reader returns rows one by one without loading the full table into memory.
            await using DbDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // Each row is stored by column name so the mapping can ask for values by name.
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
                $"Error reading table '{mapping.TableName}'. Check whether the table name and mapping fields exist in the database. Detail: {exception.Message}",
                exception);
        }
        finally
        {
            await connection.CloseAsync();
        }

        return rows;
    }

    // Reads only the column names from the source table, without reading table data.
    private static async Task<List<string>> ReadTableColumnsAsync(DbConnection connection, MappingConfiguration mapping)
    {
        string table = EscapeSqlName(mapping.TableName);
        List<string> columns = new List<string>();

        // TOP (0) returns no data, but still lets us see which columns the table has.
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT TOP (0) * FROM {table}";

        await using DbDataReader reader = await command.ExecuteReaderAsync();

        for (int index = 0; index < reader.FieldCount; index++)
        {
            columns.Add(reader.GetName(index));
        }

        return columns;
    }

    // Adds a parameter value to a DbCommand so values are not written directly into SQL text.
    private static void AddParameter(DbCommand command, string name, object value)
    {
        // Parameters keep values separate from SQL text and make the query safer.
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
