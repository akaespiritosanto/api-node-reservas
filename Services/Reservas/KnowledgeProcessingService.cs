using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

/*
================================================================================
                                Main service flow
================================================================================
 This file keeps only the main processing flow. The helper code was moved to
 smaller files with the same class name and the "partial" keyword.

 The complete flow is:
 1. Find the mapping.
 2. Read changed rows from the source database.
 3. Convert each row to a KnowledgeRecordDto.
 4. Save Nodes, Contexts and Arcs in the knowledge database.
================================================================================
*/
public partial class KnowledgeProcessingService
{
    private readonly ReservasDbContext reservasDbContext;
    private readonly OneNoteDbContext oneNoteDbContext;
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly MappingRepository mappingRepository;
    private readonly OneNoteMappingRepository oneNoteMappingRepository;
    private readonly UmbracoDbContext umbracoDbContext;
    private readonly UmbracoMappingRepository umbracoMappingRepository;
    private readonly ILogger<KnowledgeProcessingService> logger;

    public KnowledgeProcessingService(
        ReservasDbContext reservasDbContext,
        OneNoteDbContext oneNoteDbContext,
        KnowledgeDbContext knowledgeDbContext,
        MappingRepository mappingRepository,
        OneNoteMappingRepository oneNoteMappingRepository,
        UmbracoDbContext umbracoDbContext,
        UmbracoMappingRepository umbracoMappingRepository,
        ILogger<KnowledgeProcessingService> logger)
    {
        this.reservasDbContext = reservasDbContext;
        this.oneNoteDbContext = oneNoteDbContext;
        this.knowledgeDbContext = knowledgeDbContext;
        this.mappingRepository = mappingRepository;
        this.oneNoteMappingRepository = oneNoteMappingRepository;
        this.umbracoDbContext = umbracoDbContext;
        this.umbracoMappingRepository = umbracoMappingRepository;
        this.logger = logger;
    }

    // Finds a mapping by id and starts the processing for that mapping.
    public async Task<ProcessingResultDto?> ProcessMappingAsync(int mappingId, int limit)
    {
        // The controller can process a table by mapping id.
        MappingConfiguration? mapping = mappingRepository.GetById(mappingId);

        return await ProcessMappingAsync(mapping, limit);
    }

    // Finds a mapping by table name and starts the processing for that mapping.
    public async Task<ProcessingResultDto?> ProcessMappingByTableNameAsync(string tableName, int limit)
    {
        // Swagger can also process a table by name, for example "Reserva".
        MappingConfiguration? mapping = mappingRepository.GetByTableName(tableName);

        return await ProcessMappingAsync(mapping, limit);
    }

    public async Task<ProcessingResultDto?> ProcessOneNoteMappingAsync(int mappingId, int limit)
    {
        MappingConfiguration? mapping = oneNoteMappingRepository.GetById(mappingId);

        return await ProcessMappingAsync(mapping, limit, oneNoteMappingRepository);
    }

    public async Task<ProcessingResultDto?> ProcessOneNoteMappingByTableNameAsync(string tableName, int limit)
    {
        MappingConfiguration? mapping = oneNoteMappingRepository.GetByTableName(tableName);

        return await ProcessMappingAsync(mapping, limit, oneNoteMappingRepository);
    }

    public async Task<ProcessingResultDto?> ProcessUmbracoMappingAsync(int mappingId, int limit)
    {
        MappingConfiguration? mapping = umbracoMappingRepository.GetById(mappingId);
        if (mapping is null) return null;

        return await ProcessUmbracoMappingInternalAsync(mapping, limit);
    }

    public async Task<ProcessingResultDto?> ProcessUmbracoMappingByTableNameAsync(string tableName, int limit)
    {
        MappingConfiguration? mapping = umbracoMappingRepository.GetByTableName(tableName);
        if (mapping is null) return null;

        return await ProcessUmbracoMappingInternalAsync(mapping, limit);
    }

    // Runs the complete processing flow after the mapping has already been found.
    private async Task<ProcessingResultDto?> ProcessMappingAsync(MappingConfiguration? mapping, int limit)
    {
        return await ProcessMappingAsync(mapping, limit, mappingRepository);
    }

    private async Task<ProcessingResultDto?> ProcessMappingAsync(
        MappingConfiguration? mapping,
        int limit,
        object processingStateRepository)
    {
        if (mapping is null)
        {
            logger.LogWarning("Mapping was not found.");
            return null;
        }

        logger.LogInformation("Processing mapping {MappingId} for table {TableName}.", mapping.Id, mapping.TableName);

        ValidateMapping(mapping);
        await PrepareKnowledgeDatabaseAsync();

        // The rows are dictionaries because every mapping can use different column names.
        // Choose the database connection according to the mapping repository type.
        System.Data.Common.DbConnection connection = reservasDbContext.Database.GetDbConnection();

        if (processingStateRepository is UmbracoMappingRepository)
        {
            // Ensure Umbraco DB connection is configured before attempting to read.
            var dbConnection = umbracoDbContext.Database.GetDbConnection();
            if (string.IsNullOrWhiteSpace(dbConnection.ConnectionString))
            {
                throw new InvalidOperationException("Umbraco DB connection string is not configured. Set UMBRACO_DB_CONNECTION_STRING or provide a default DB_CONNECTION_STRING in .env or environment variables.");
            }

            connection = dbConnection;
        }

        if (processingStateRepository is OneNoteMappingRepository)
        {
            // OneNote imports are stored in their own staging database.
            connection = oneNoteDbContext.Database.GetDbConnection();
        }

        List<Dictionary<string, object?>> rows = await ReadRowsToProcessAsync(mapping, limit, connection);
        List<KnowledgeRecordDto> records = new List<KnowledgeRecordDto>();

        // These values are returned to Swagger so the user can see what happened.
        int lastProcessedId = mapping.LastProcessedId;
        int nodesCreated = 0;
        int nodesUpdated = 0;
        int contextsCreated = 0;
        int arcsCreated = 0;

        foreach (Dictionary<string, object?> row in rows)
        {
            // Convert the source table row into the common format used by the saving code.
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

        // The checkpoint prevents the same old rows from being processed every time.
        DateTime processingDate = DateTime.UtcNow;
        UpdateProcessingState(processingStateRepository, mapping.Id, lastProcessedId, processingDate);

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

    private static void UpdateProcessingState(object repository, int mappingId, int lastProcessedId, DateTime processingDate)
    {
        if (repository is OneNoteMappingRepository oneNoteRepository)
        {
            oneNoteRepository.UpdateProcessingState(mappingId, lastProcessedId, processingDate);
            return;
        }
        if (repository is UmbracoMappingRepository umbracoRepository)
        {
            umbracoRepository.UpdateProcessingState(mappingId, lastProcessedId, processingDate);
            return;
        }
        if (repository is MappingRepository normalRepository)
        {
            normalRepository.UpdateProcessingState(mappingId, lastProcessedId, processingDate);
        }
    }

    private class SaveResult
    {
        public bool NodeCreated { get; set; }
        public bool NodeUpdated { get; set; }
        public int ContextsCreated { get; set; }
        public int ArcsCreated { get; set; }
    }
}
