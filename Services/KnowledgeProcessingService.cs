using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

public class KnowledgeProcessingService
{
    private readonly ReservasDbContext reservasDbContext;
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly MappingRepository mappingRepository;
    private readonly KnowledgeTableService tableService;
    private static readonly Regex SafeSqlName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public KnowledgeProcessingService(
        ReservasDbContext reservasDbContext,
        KnowledgeDbContext knowledgeDbContext,
        MappingRepository mappingRepository,
        KnowledgeTableService tableService)
    {
        this.reservasDbContext = reservasDbContext;
        this.knowledgeDbContext = knowledgeDbContext;
        this.mappingRepository = mappingRepository;
        this.tableService = tableService;
    }

    public async Task<ProcessingResultDto?> ProcessMappingAsync(int mappingId, int limit)
    {
        MappingConfiguration? mapping = mappingRepository.GetById(mappingId);

        if (mapping is null)
        {
            return null;
        }

        ValidateMapping(mapping);
        await tableService.CreateTablesIfNeededAsync();

        List<Dictionary<string, object?>> rows = await ReadRowsToProcessAsync(mapping, limit);
        List<KnowledgeRecordDto> records = [];
        int lastProcessedId = mapping.LastProcessedId;
        int nodesCreated = 0;
        int nodesUpdated = 0;
        int contextsCreated = 0;
        int arcsCreated = 0;

        foreach (Dictionary<string, object?> row in rows)
        {
            KnowledgeRecordDto record = ConvertRowToKnowledgeRecord(mapping, row);
            records.Add(record);

            SaveResult saveResult = await SaveKnowledgeRecordAsync(record);
            nodesCreated += saveResult.NodeCreated ? 1 : 0;
            nodesUpdated += saveResult.NodeUpdated ? 1 : 0;
            contextsCreated += saveResult.ContextsCreated;
            arcsCreated += saveResult.ArcsCreated;

            if (record.SourceId > lastProcessedId)
            {
                lastProcessedId = record.SourceId;
            }
        }

        DateTime processingDate = DateTime.UtcNow;
        mappingRepository.UpdateProcessingState(mapping.Id, lastProcessedId, processingDate);

        return new ProcessingResultDto
        {
            MappingId = mapping.Id,
            TableName = mapping.TableName,
            RecordsProcessed = records.Count,
            NodesCreated = nodesCreated,
            NodesUpdated = nodesUpdated,
            ContextsCreated = contextsCreated,
            ArcsCreated = arcsCreated,
            LastProcessedId = lastProcessedId,
            ProcessingDate = processingDate,
            Records = records
        };
    }

    public async Task CreateKnowledgeTablesAsync()
    {
        await tableService.CreateTablesIfNeededAsync();
    }

    private async Task<List<Dictionary<string, object?>>> ReadRowsToProcessAsync(MappingConfiguration mapping, int limit)
    {
        List<Dictionary<string, object?>> rows = [];
        DbConnection connection = reservasDbContext.Database.GetDbConnection();

        try
        {
            await connection.OpenAsync();

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = BuildSql(mapping);

            DbParameter limitParameter = command.CreateParameter();
            limitParameter.ParameterName = "@limit";
            limitParameter.Value = limit;
            command.Parameters.Add(limitParameter);

            DbParameter lastIdParameter = command.CreateParameter();
            lastIdParameter.ParameterName = "@lastId";
            lastIdParameter.Value = mapping.LastProcessedId;
            command.Parameters.Add(lastIdParameter);

            DbParameter lastDateParameter = command.CreateParameter();
            lastDateParameter.ParameterName = "@lastDate";
            lastDateParameter.Value = mapping.LastSuccessfulProcessingDate ?? new DateTime(1900, 1, 1);
            command.Parameters.Add(lastDateParameter);

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
        finally
        {
            await connection.CloseAsync();
        }

        return rows;
    }

    private async Task<SaveResult> SaveKnowledgeRecordAsync(KnowledgeRecordDto record)
    {
        DateTime now = DateTime.UtcNow;
        SaveResult result = new();

        Node? node = await knowledgeDbContext.Nodes.FirstOrDefaultAsync(existingNode =>
            existingNode.SourceTable == record.SourceTable &&
            existingNode.SourceId == record.SourceId);

        if (node is null)
        {
            node = new Node
            {
                SourceTable = record.SourceTable,
                SourceId = record.SourceId,
                DataCriacao = now
            };

            knowledgeDbContext.Nodes.Add(node);
            result.NodeCreated = true;
        }
        else
        {
            result.NodeUpdated = true;
        }

        node.Tipo = LimitText(record.Tipo, 100);
        node.TipoE = LimitText(record.TipoE, 100);
        node.Descricao = LimitText(record.Descricao, 2000);
        node.IdInformacao = LimitText(record.IdInformacao, 200);
        node.Par1 = LimitText(record.Par1, 1000);
        node.Par2 = LimitText(record.Par2, 1000);
        node.Par3 = LimitText(record.Par3, 1000);
        node.Par4 = LimitText(record.Par4, 1000);
        node.Par5 = LimitText(record.Par5, 1000);
        node.Par6 = LimitText(record.Par6, 1000);
        node.Par7 = LimitText(record.Par7, 1000);
        node.DataActualizacao = now;

        await knowledgeDbContext.SaveChangesAsync();

        List<Context> oldContexts = await knowledgeDbContext.Contexts.Where(context => context.NodeId == node.Id).ToListAsync();
        List<Arc> oldArcs = await knowledgeDbContext.Arcs.Where(arc => arc.NodeId == node.Id).ToListAsync();

        knowledgeDbContext.Contexts.RemoveRange(oldContexts);
        knowledgeDbContext.Arcs.RemoveRange(oldArcs);

        foreach (string contextValue in record.Contexts)
        {
            if (string.IsNullOrWhiteSpace(contextValue))
            {
                continue;
            }

            knowledgeDbContext.Contexts.Add(new Context
            {
                NodeId = node.Id,
                Valor = LimitText(contextValue, 1000),
                DataCriacao = now
            });

            result.ContextsCreated++;
        }

        foreach (KnowledgeRelationDto relation in record.Relations)
        {
            if (string.IsNullOrWhiteSpace(relation.TargetId))
            {
                continue;
            }

            knowledgeDbContext.Arcs.Add(new Arc
            {
                NodeId = node.Id,
                Tipo = LimitText(relation.Type, 100),
                TargetId = LimitText(relation.TargetId, 200),
                DataCriacao = now
            });

            result.ArcsCreated++;
        }

        await knowledgeDbContext.SaveChangesAsync();

        return result;
    }

    private static string BuildSql(MappingConfiguration mapping)
    {
        string table = EscapeSqlName(mapping.TableName);
        string idField = EscapeSqlName(mapping.IdFieldName);
        string creationField = EscapeSqlName(mapping.CreationDateFieldName);
        string updateField = EscapeSqlName(mapping.UpdateDateFieldName);

        string whereClause;

        if (mapping.DetectionMethod.Equals("CreationDate", StringComparison.OrdinalIgnoreCase))
        {
            whereClause = $"{creationField} > @lastDate OR {updateField} > @lastDate";
        }
        else
        {
            whereClause = $"{idField} > @lastId OR {updateField} > @lastDate";
        }

        return $"SELECT TOP (@limit) * FROM {table} WHERE {whereClause} ORDER BY {idField}";
    }

    private static KnowledgeRecordDto ConvertRowToKnowledgeRecord(MappingConfiguration mapping, Dictionary<string, object?> row)
    {
        return new KnowledgeRecordDto
        {
            SourceTable = mapping.TableName,
            SourceId = Convert.ToInt32(GetValue(row, mapping.IdFieldName) ?? 0),
            Tipo = GetMappedValue(row, mapping.Mapping.Tipo),
            TipoE = GetMappedValue(row, mapping.Mapping.TipoE),
            Descricao = GetMappedValue(row, mapping.Mapping.Descricao),
            IdInformacao = GetMappedValue(row, mapping.Mapping.IdInformacao),
            Par1 = GetMappedValue(row, mapping.Mapping.Par1),
            Par2 = GetMappedValue(row, mapping.Mapping.Par2),
            Par3 = GetMappedValue(row, mapping.Mapping.Par3),
            Par4 = GetMappedValue(row, mapping.Mapping.Par4),
            Par5 = GetMappedValue(row, mapping.Mapping.Par5),
            Par6 = GetMappedValue(row, mapping.Mapping.Par6),
            Par7 = GetMappedValue(row, mapping.Mapping.Par7),
            Contexts = mapping.Mapping.Contexts.Select(context => GetMappedValue(row, context)).ToList(),
            Parent = mapping.Mapping.Parent.Select(parent => GetMappedValue(row, parent)).ToList(),
            Relations = mapping.Mapping.Relations.Select(relation => new KnowledgeRelationDto
            {
                Type = relation.Type,
                TargetId = GetMappedValue(row, relation.TargetId)
            }).ToList()
        };
    }

    private static string GetMappedValue(Dictionary<string, object?> row, string mappingValue)
    {
        if (string.IsNullOrWhiteSpace(mappingValue))
        {
            return string.Empty;
        }

        object? columnValue = GetValue(row, mappingValue);

        if (columnValue is null)
        {
            return mappingValue;
        }

        return Convert.ToString(columnValue) ?? string.Empty;
    }

    private static object? GetValue(Dictionary<string, object?> row, string columnName)
    {
        if (row.TryGetValue(columnName, out object? value))
        {
            return value;
        }

        return null;
    }

    private static void ValidateMapping(MappingConfiguration mapping)
    {
        EscapeSqlName(mapping.TableName);
        EscapeSqlName(mapping.IdFieldName);
        EscapeSqlName(mapping.CreationDateFieldName);
        EscapeSqlName(mapping.UpdateDateFieldName);
    }

    private static string EscapeSqlName(string name)
    {
        if (!SafeSqlName.IsMatch(name))
        {
            throw new InvalidOperationException($"Nome SQL invalido: {name}");
        }

        return $"[{name}]";
    }

    private static string LimitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength];
    }

    private class SaveResult
    {
        public bool NodeCreated { get; set; }
        public bool NodeUpdated { get; set; }
        public int ContextsCreated { get; set; }
        public int ArcsCreated { get; set; }
    }
}
