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

    // Makes sure the Context table is prepared.
    // The parent column has been replaced by the Location column, so it is no longer recreated.
    private Task PrepareKnowledgeDatabaseAsync()
    {
        return Task.CompletedTask;
    }
}
