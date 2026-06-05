using api_node_reservas.Dtos;
using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class KnowledgeProcessingService
{
    /*
    ============================================================================
                                  Context saving
    ============================================================================
     Context rows are extra text values connected to a Node, such as status,
     payment state or notebook name.
    ============================================================================
    */

    // Creates the Context rows for one Node.
    private void AddContexts(Node node, KnowledgeRecordDto record, DateTime updateDate, SaveResult result)
    {
        // Location is the order of the context value for this Node: 1, 2, 3, ...
        int location = 1;

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
                Location = location,
                Par1 = null,
                UpdateDate = updateDate,
                DescriptionType = null
            });

            location++;
            result.ContextsCreated++;
        }
    }
}
