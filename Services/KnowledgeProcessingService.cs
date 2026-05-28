using api_node_reservas.Data;
using api_node_reservas.Dtos;
using api_node_reservas.Models;

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
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly MappingRepository mappingRepository;
    private readonly ILogger<KnowledgeProcessingService> logger;

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

    // Runs the complete processing flow after the mapping has already been found.
    private async Task<ProcessingResultDto?> ProcessMappingAsync(MappingConfiguration? mapping, int limit)
    {
        if (mapping is null)
        {
            logger.LogWarning("Mapping was not found.");
            return null;
        }

        logger.LogInformation("Processing mapping {MappingId} for table {TableName}.", mapping.Id, mapping.TableName);

        ValidateMapping(mapping);

        // The rows are dictionaries because every mapping can use different column names.
        List<Dictionary<string, object?>> rows = await ReadRowsToProcessAsync(mapping, limit);
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

    private class SaveResult
    {
        public bool NodeCreated { get; set; }
        public bool NodeUpdated { get; set; }
        public int ContextsCreated { get; set; }
        public int ArcsCreated { get; set; }
    }
}
