using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                               Mapping validation
    ============================================================================
     This file keeps the first safety checks for a mapping. More detailed column
     checks are in KnowledgeProcessingService.TableColumns.cs, and column choice
     for SELECT is in KnowledgeProcessingService.ColumnSelection.cs.
    ============================================================================
    */

    // Checks if the main SQL names in the mapping are safe to use.
    private static void ValidateMapping(MappingConfiguration mapping)
    {
        EscapeSqlName(mapping.TableName);
        EscapeSqlName(mapping.IdFieldName);
        EscapeSqlName(mapping.CreationDateFieldName);
        EscapeSqlName(mapping.UpdateDateFieldName);
    }
}
