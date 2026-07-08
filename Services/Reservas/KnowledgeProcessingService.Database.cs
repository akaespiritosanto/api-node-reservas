using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                              Knowledge DB preparation
    ============================================================================
     Older knowledge databases can miss columns that newer mapping features use.
     These helpers add only the missing columns that the API needs to process.
    ============================================================================
    */

    // Makes sure the Node table has the OneNote date fields used by sync.
    private async Task PrepareKnowledgeDatabaseAsync()
    {
        string sql = @"
IF COL_LENGTH('dbo.Node', 'LastModifiedDateTime') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD LastModifiedDateTime DATETIME2 NULL;
END

IF COL_LENGTH('dbo.Node', 'ImportedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD ImportedAt DATETIME2 NULL;
END

IF COL_LENGTH('dbo.Node', 'syncStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Node ADD syncStatus VARCHAR(50) NULL;
END";

        await knowledgeDbContext.Database.ExecuteSqlRawAsync(sql);
    }
}
