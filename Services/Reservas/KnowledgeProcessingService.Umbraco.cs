using api_node_reservas.Dtos;
using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    // Custom mapping processor for Umbraco that uses EF Core models instead of raw SQL SELECT statements.
    private async Task<ProcessingResultDto?> ProcessUmbracoMappingInternalAsync(MappingConfiguration mapping, int limit)
    {
        if (mapping is null)
        {
            logger.LogWarning("Mapping was not found.");
            return null;
        }

        logger.LogInformation("Processing Umbraco mapping {MappingId} for table {TableName} using EF Core models.", mapping.Id, mapping.TableName);

        await PrepareKnowledgeDatabaseAsync();

        int lastProcessedId = mapping.LastProcessedId;
        int nodesCreated = 0;
        int nodesUpdated = 0;
        int contextsCreated = 0;
        int arcsCreated = 0;

        DateTime now = DateTime.UtcNow;

        if (mapping.TableName.Equals("cmsDocument", StringComparison.OrdinalIgnoreCase))
        {
            // C# LINQ query using EF Core models joining cmsDocument, umbracoNode, cmsContent and cmsContentType
            var query = from d in umbracoDbContext.CmsDocuments
                        join n in umbracoDbContext.UmbracoNodes on d.nodeId equals n.id
                        join c in umbracoDbContext.CmsContent on d.nodeId equals c.nodeId
                        join ct in umbracoDbContext.CmsContentTypes on c.contentType equals ct.nodeId
                        where d.nodeId > mapping.LastProcessedId
                        orderby d.nodeId
                        select new
                        {
                            NodeId = d.nodeId,
                            Text = d.text,
                            UpdateDate = d.updateDate,
                            Alias = ct.alias,
                            ParentId = n.parentID
                        };

            var items = await query.Take(limit).ToListAsync();

            foreach (var item in items)
            {
                var record = new KnowledgeRecordDto
                {
                    SourceTable = "cmsDocument",
                    SourceId = item.NodeId,
                    Tipo = item.Alias ?? string.Empty,
                    TipoE = "Document",
                    Reference = item.Text,
                    Descricao = string.Empty,
                    IdInformacao = item.NodeId.ToString(),
                    Parent = new List<string> { item.ParentId.ToString() }
                };

                SaveResult saveResult = await SaveKnowledgeRecordAsync(record);
                if (saveResult.NodeCreated) nodesCreated++;
                if (saveResult.NodeUpdated) nodesUpdated++;
                contextsCreated += saveResult.ContextsCreated;
                arcsCreated += saveResult.ArcsCreated;

                lastProcessedId = item.NodeId;
            }
        }
        else if (mapping.TableName.Equals("cmsContent", StringComparison.OrdinalIgnoreCase))
        {
            // C# LINQ query using EF Core models joining cmsContent, umbracoNode and cmsContentType
            var query = from c in umbracoDbContext.CmsContent
                        join n in umbracoDbContext.UmbracoNodes on c.nodeId equals n.id
                        join ct in umbracoDbContext.CmsContentTypes on c.contentType equals ct.nodeId
                        where c.nodeId > mapping.LastProcessedId
                        orderby c.nodeId
                        select new
                        {
                            NodeId = c.nodeId,
                            Text = n.text,
                            CreateDate = n.createDate,
                            Alias = ct.alias,
                            ParentId = n.parentID
                        };

            var items = await query.Take(limit).ToListAsync();

            foreach (var item in items)
            {
                var record = new KnowledgeRecordDto
                {
                    SourceTable = "cmsContent",
                    SourceId = item.NodeId,
                    Tipo = item.Alias ?? string.Empty,
                    TipoE = "Content",
                    Reference = item.Text ?? string.Empty,
                    Descricao = string.Empty,
                    IdInformacao = item.NodeId.ToString(),
                    Parent = new List<string> { item.ParentId.ToString() }
                };

                SaveResult saveResult = await SaveKnowledgeRecordAsync(record);
                if (saveResult.NodeCreated) nodesCreated++;
                if (saveResult.NodeUpdated) nodesUpdated++;
                contextsCreated += saveResult.ContextsCreated;
                arcsCreated += saveResult.ArcsCreated;

                lastProcessedId = item.NodeId;
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported Umbraco table mapping: {mapping.TableName}");
        }

        if (lastProcessedId > mapping.LastProcessedId)
        {
            umbracoMappingRepository.UpdateProcessingState(mapping.Id, lastProcessedId, now);
        }

        return new ProcessingResultDto
        {
            TableName = mapping.TableName,
            LastProcessedId = lastProcessedId,
            NodesCreated = nodesCreated,
            NodesUpdated = nodesUpdated,
            ContextsCreated = contextsCreated,
            ArcsCreated = arcsCreated
        };
    }
}
