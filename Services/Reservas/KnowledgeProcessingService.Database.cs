using Microsoft.Data.SqlClient;
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

    // Makes sure the Context table has the parent column used by context trees.
    private async Task PrepareKnowledgeDatabaseAsync()
    {
        try
        {
            string sql = @"
IF OBJECT_ID('dbo.Context', 'U') IS NOT NULL
AND COL_LENGTH('dbo.Context', 'parent') IS NULL
BEGIN
    ALTER TABLE dbo.[Context] ADD [parent] INT NOT NULL DEFAULT 0;
END";

            await knowledgeDbContext.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException exception)
        {
            throw new InvalidOperationException(
                $"Error preparing the knowledge database. Check whether the SQL user can alter the Context table. Detail: {exception.Message}",
                exception);
        }
    }
}
