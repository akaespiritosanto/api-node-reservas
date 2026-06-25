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
        // Provide clearer errors for beginners when mapping fields are missing.
        if (string.IsNullOrWhiteSpace(mapping.TableName))
        {
            throw new InvalidOperationException("Mapping TableName is empty. Check the mapping configuration (TableName).");
        }

        if (string.IsNullOrWhiteSpace(mapping.IdFieldName))
        {
            throw new InvalidOperationException($"Mapping for table '{mapping.TableName}' has empty IdFieldName.");
        }

        // Use the existing escape checks to validate allowed SQL names.
        EscapeSqlName(mapping.TableName);
        EscapeSqlName(mapping.IdFieldName);

        if (!string.IsNullOrWhiteSpace(mapping.CreationDateFieldName))
        {
            EscapeSqlName(mapping.CreationDateFieldName);
        }

        if (!string.IsNullOrWhiteSpace(mapping.UpdateDateFieldName))
        {
            EscapeSqlName(mapping.UpdateDateFieldName);
        }
    }
}
