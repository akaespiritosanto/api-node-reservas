using api_node_reservas.Data;

namespace api_node_reservas.Services;

/*
================================================================================
                              OneNote synchronization
================================================================================
 This service contains the write side of the OneNote flow:
 - create and rename OneNote sections;
 - change the content of a OneNote page;
 - attach a file to a OneNote page;
 - synchronize one Node with its OneNote page.

 Beginner note: moving notes between sections or notebooks is intentionally not
 implemented here because that was excluded from the first version.

 The code is split into partial files so each file has one simple job.
================================================================================
*/
public partial class OneNoteSyncService
{
    private readonly KnowledgeDbContext knowledgeDbContext;
    private readonly OneNoteTokenStore tokenStore;
    private readonly HttpClient httpClient;

    public OneNoteSyncService(
        KnowledgeDbContext knowledgeDbContext,
        OneNoteTokenStore tokenStore,
        HttpClient httpClient)
    {
        this.knowledgeDbContext = knowledgeDbContext;
        this.tokenStore = tokenStore;
        this.httpClient = httpClient;
    }
}
