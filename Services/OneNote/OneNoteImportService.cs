using api_node_reservas.Data;
using api_node_reservas.Dtos;

namespace api_node_reservas.Services;

/*
================================================================================
                              OneNote import
================================================================================
 This service reads OneNote pages from Microsoft Graph and saves them into a
 simple source table named OneNotePageImport.

 The code is divided into smaller files with the same class name:
 - OneNoteImportService.Graph.cs reads data from Microsoft Graph.
 - OneNoteImportService.Database.cs saves pages into SQL Server.
 - OneNoteImportService.Json.cs reads simple values from Graph JSON.
 - OneNoteImportService.PageData.cs stores one imported page in memory.

 After this import step, the normal mapping processor can read OneNotePageImport
 and save the information into the knowledge database as Node, Context and Arc.
================================================================================
*/
public partial class OneNoteImportService
{
    private readonly OneNoteDbContext oneNoteDbContext;
    private readonly MicrosoftGraphAuthService authService;
    private readonly OneNoteTokenStore tokenStore;
    private readonly HttpClient httpClient;

    public OneNoteImportService(
        OneNoteDbContext oneNoteDbContext,
        MicrosoftGraphAuthService authService,
        OneNoteTokenStore tokenStore,
        HttpClient httpClient)
    {
        this.oneNoteDbContext = oneNoteDbContext;
        this.authService = authService;
        this.tokenStore = tokenStore;
        this.httpClient = httpClient;
    }

    // Imports OneNote pages into the local staging table.
    public async Task<OneNoteImportResultDto> ImportAsync(OneNoteImportRequestDto request)
    {
        string accessToken = await GetAccessTokenForImportAsync(request);

        await CreateImportTableIfMissingAsync();

        string userId = await ReadCurrentUserIdAsync(accessToken);
        List<OneNotePageData> pages = await ReadPagesAsync(accessToken, userId, request.Limit);

        int savedPages = 0;

        foreach (OneNotePageData page in pages)
        {
            await SavePageAsync(page);
            savedPages++;
        }

        return new OneNoteImportResultDto
        {
            PagesRead = pages.Count,
            PagesSaved = savedPages
        };
    }

    // Gets a token from the request, from an authorization code, or from memory.
    private async Task<string> GetAccessTokenForImportAsync(OneNoteImportRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return request.AccessToken;
        }

        if (!string.IsNullOrWhiteSpace(request.AuthorizationCode))
        {
            OneNoteTokenDto token = await authService.ExchangeCodeForTokenAsync(request.AuthorizationCode, request.RedirectUri);
            tokenStore.SaveToken(token.AccessToken, token.ExpiresIn);
            return token.AccessToken;
        }

        string storedToken = tokenStore.GetAccessToken();

        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            return storedToken;
        }

        throw new InvalidOperationException("Login with Microsoft first, or send an AccessToken or AuthorizationCode.");
    }
}
