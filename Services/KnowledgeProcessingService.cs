using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace api_node_reservas.Services;

/*
================================================================================
|                        KnowledgeProcessingService                            |
================================================================================
| Este servico faz o trabalho principal do projeto.                             |
|                                                                              |
| 1. Le um mapeamento.                                                          |
| 2. Vai buscar apenas as linhas novas ou alteradas na base de reservas.        |
| 3. Converte cada linha para um KnowledgeRecordDto.                            |
| 4. Guarda ou atualiza os dados na base de conhecimento.                       |
|                                                                              |
| A identidade de um Node e IdInformacao + Tipo. Isto permite ter dois registos |
| com o mesmo IdInformacao, desde que tenham Tipos diferentes.                  |
================================================================================
*/
public class KnowledgeProcessingService
{
    private readonly ReservasDbContext reservasDbContext;
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly MappingRepository mappingRepository;
    private readonly ILogger<KnowledgeProcessingService> logger;
    private static readonly Regex SafeSqlName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public KnowledgeProcessingService(
        ReservasDbContext reservasDbContext,
        KnowledgeDbContext knowledgeDbContext,
        MappingRepository mappingRepository,
        ILogger<KnowledgeProcessingService> logger)
    {
        this.reservasDbContext = reservasDbContext;
        this.knowledgeDbContext = knowledgeDbContext;
        this.mappingRepository = mappingRepository;
        this.logger = logger;
    }

    public async Task<ProcessingResultDto?> ProcessMappingAsync(int mappingId, int limit)
    {
        /*
        ========================================================================
        |                          ProcessMappingAsync                          |
        ========================================================================
        | Metodo chamado pelo controller quando o utilizador pede para processar|
        | um mapeamento. Ele devolve um resumo com o que foi criado/atualizado. |
        ========================================================================
        */
        MappingConfiguration? mapping = mappingRepository.GetById(mappingId);

        if (mapping is null)
        {
            logger.LogWarning("Mapping {MappingId} was not found.", mappingId);
            return null;
        }

        logger.LogInformation("Processing mapping {MappingId} for table {TableName}.", mapping.Id, mapping.TableName);

        ValidateMapping(mapping);

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

        logger.LogInformation("Finished mapping {MappingId}. Processed {RecordCount} records.", mapping.Id, records.Count);

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

    private async Task<List<Dictionary<string, object?>>> ReadRowsToProcessAsync(MappingConfiguration mapping, int limit)
    {
        /*
        ========================================================================
        |                          ReadRowsToProcessAsync                       |
        ========================================================================
        | Le os registos da tabela de origem.                                   |
        |                                                                        |
        | Em vez de usar SELECT *, primeiro descobre as colunas reais da tabela |
        | e depois seleciona so as colunas que o mapeamento precisa.            |
        ========================================================================
        */
        List<Dictionary<string, object?>> rows = [];
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

    private async Task<SaveResult> SaveKnowledgeRecordAsync(KnowledgeRecordDto record)
    {
        /*
        ========================================================================
        |                          SaveKnowledgeRecordAsync                     |
        ========================================================================
        | Guarda um registo na base de conhecimento.                            |
        |                                                                        |
        | Primeiro procura um Node com o mesmo IdInformacao e Tipo. Se existir, |
        | atualiza. Se nao existir, cria um novo.                               |
        ========================================================================
        */
        DateTime now = DateTime.UtcNow;
        SaveResult result = new();
        int typeId = ConvertIdInformacaoToInt(record.IdInformacao);
        string type = LimitText(record.Tipo, 30);

        Node? node = await knowledgeDbContext.Nodes
            .FirstOrDefaultAsync(existingNode => existingNode.TypeId == typeId && existingNode.Type == type);

        if (node is null)
        {
            node = new Node
            {
                Reference = LimitText(record.Reference, 1000),
                TypeId = typeId,
                Type = type
            };

            knowledgeDbContext.Nodes.Add(node);
            result.NodeCreated = true;
        }
        else
        {
            result.NodeUpdated = true;
        }

        FillNode(node, record, now);

        try
        {
            await knowledgeDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Erro ao gravar o Node na base de conhecimento. Confirma se a tabela Node aceita TypeId + Type como identificador e se ja nao existe uma restricao antiga unica no campo reference.",
                exception);
        }

        List<Context> oldContexts = await knowledgeDbContext.Contexts.Where(context => context.NodeId == node.Id).ToListAsync();
        List<Arc> oldArcs = await knowledgeDbContext.Arcs.Where(arc => arc.Source == node.Id).ToListAsync();

        knowledgeDbContext.Contexts.RemoveRange(oldContexts);
        knowledgeDbContext.Arcs.RemoveRange(oldArcs);

/*
        foreach (string contextValue in record.Contexts)
        {
            if (string.IsNullOrWhiteSpace(contextValue))
            {
                continue;
            }

            knowledgeDbContext.Contexts.Add(new Context
            {
                NodeId = node.Id,
                Description = LimitText(contextValue, 8000),
                Location = 0,
                Par1 = null,
                UpdateDate = now,
                DescriptionType = null
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
                Source = node.Id,
                Target = ConvertTargetToInt(relation.TargetId),
                TypeId = 0,
                Type = LimitText(relation.Type, 50),
                UpdateDate = now
            });

            result.ArcsCreated++;
        }
*/
        try
        {
            await knowledgeDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Erro ao gravar Contexts ou Arcs na base de conhecimento. Confirma se os campos source, target, typeId e nodeId aceitam os valores gerados pelo processamento.",
                exception);
        }

        return result;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void FillNode(Node node, KnowledgeRecordDto record, DateTime updateDate)
    {
        node.Reference = LimitText(record.Reference, 1000);
        node.TypeId = ConvertIdInformacaoToInt(record.IdInformacao);
        node.Type = LimitText(record.Tipo, 30);
        node.Description = LimitText(record.Descricao, 8000);
        node.Par1 = LimitText(record.Par1, 200);
        node.Par2 = LimitText(record.Par2, 200);
        node.Par3 = LimitText(record.Par3, 200);
        node.Par4 = LimitText(record.Par4, 200);
        node.Par5 = LimitText(record.Par5, 200);
        node.Par6 = LimitText(record.Par6, 200);
        node.Par7 = LimitText(record.Par7, 200);
        node.Link = string.Empty;
        node.Security = 0;
        node.UpdateDate = updateDate;
        node.UpdateUser = 0;
        node.DescriptionType = null;
    }

    private static int ConvertTargetToInt(string targetId)
    {
        if (int.TryParse(targetId, out int value))
        {
            return value;
        }

        return 0;
    }

    private static int ConvertIdInformacaoToInt(string idInformacao)
    {
        if (int.TryParse(idInformacao, out int value))
        {
            return value;
        }

        return 0;
    }

    private static string BuildSql(MappingConfiguration mapping, List<string> tableColumns)
    {
        /*
        ========================================================================
        |                                BuildSql                               |
        ========================================================================
        | Constroi a query SQL.                                                 |
        |                                                                        |
        | A lista selectedColumns evita SELECT * e impede que a API carregue    |
        | colunas que nao sao usadas pelo mapeamento.                           |
        ========================================================================
        */
        string table = EscapeSqlName(mapping.TableName);
        string idField = EscapeSqlName(mapping.IdFieldName);
        string creationField = EscapeSqlName(mapping.CreationDateFieldName);
        string updateField = EscapeSqlName(mapping.UpdateDateFieldName);
        List<string> selectedColumns = GetColumnsUsedByMapping(mapping, tableColumns);

        if (selectedColumns.Count == 0)
        {
            throw new InvalidOperationException($"O mapeamento da tabela '{mapping.TableName}' nao tem nenhuma coluna valida para selecionar.");
        }

        string selectClause = string.Join(", ", selectedColumns.Select(EscapeSqlName));

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

    private static KnowledgeRecordDto ConvertRowToKnowledgeRecord(MappingConfiguration mapping, Dictionary<string, object?> row)
    {
        return new KnowledgeRecordDto
        {
            SourceTable = mapping.TableName,
            SourceId = Convert.ToInt32(GetValue(row, mapping.IdFieldName) ?? 0),
            Tipo = GetMappedValue(row, mapping.Mapping.Tipo),
            TipoE = GetMappedValue(row, mapping.Mapping.TipoE),
            Reference = GetMappedValue(row, mapping.Mapping.Reference),
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

    private static List<string> GetColumnsUsedByMapping(MappingConfiguration mapping, List<string> tableColumns)
    {
        List<string> selectedColumns = [];

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
        }

        return selectedColumns;
    }

    private static void ValidateTableColumns(MappingConfiguration mapping, List<string> tableColumns)
    {
        /*
        ========================================================================
        |                           ValidateTableColumns                        |
        ========================================================================
        | Confirma se as colunas obrigatorias existem mesmo na tabela de origem.|
        |                                                                        |
        | Isto evita respostas genericas como "Erro interno no servidor" quando |
        | o problema e apenas um nome errado no mapeamento.                     |
        ========================================================================
        */
        List<string> missingColumns = [];

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
        return tableColumns.Any(column => column.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFixedValueField(string fieldName)
    {
        return fieldName.Equals("tipo", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("tipoE", StringComparison.OrdinalIgnoreCase);
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

        string? realColumnName = tableColumns.FirstOrDefault(column =>
            column.Equals(possibleColumn, StringComparison.OrdinalIgnoreCase));

        if (realColumnName is null)
        {
            return;
        }

        if (selectedColumns.Any(column => column.Equals(realColumnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        selectedColumns.Add(realColumnName);
    }

    private static async Task<List<string>> ReadTableColumnsAsync(DbConnection connection, MappingConfiguration mapping)
    {
        string table = EscapeSqlName(mapping.TableName);
        List<string> columns = [];

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT TOP (0) * FROM {table}";

        await using DbDataReader reader = await command.ExecuteReaderAsync();

        for (int index = 0; index < reader.FieldCount; index++)
        {
            columns.Add(reader.GetName(index));
        }

        return columns;
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
