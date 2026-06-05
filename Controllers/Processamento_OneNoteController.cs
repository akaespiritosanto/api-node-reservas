using api_node_reservas.Dtos;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_node_reservas.Controllers;

/*
================================================================================
                             OneNote processing API
================================================================================
 These endpoints help the user connect to Microsoft OneNote, import notes into
 the staging table and process those imported notes into the knowledge database.
================================================================================
*/
[ApiController]
[Route("api/onenote")]
[Produces("application/json")]
public class Processamento_OneNoteController : ControllerBase
{
    private readonly MicrosoftGraphAuthService authService;
    private readonly OneNoteImportService importService;
    private readonly OneNoteTokenStore tokenStore;
    private readonly KnowledgeProcessingService processingService;

    // Receives the services used by the OneNote flow:
    // login, import, temporary token storage and final KB processing.
    public Processamento_OneNoteController(
        MicrosoftGraphAuthService authService,
        OneNoteImportService importService,
        OneNoteTokenStore tokenStore,
        KnowledgeProcessingService processingService)
    {
        this.authService = authService;
        this.importService = importService;
        this.tokenStore = tokenStore;
        this.processingService = processingService;
    }

    /// <summary>
    /// Creates the Microsoft login URL used to request OneNote access.
    /// </summary>
    [HttpGet("login-url")]
    [ProducesResponseType(typeof(OneNoteAuthUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Step 1: the user opens this URL to sign in with Microsoft.
    public ActionResult<OneNoteAuthUrlDto> GetLoginUrl()
    {
        return Ok(authService.CreateAuthorizationUrl());
    }

    /// <summary>
    /// Receives the Microsoft login result and stores the OneNote access token temporarily.
    /// </summary>
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Step 2: Microsoft redirects here after login. This endpoint stores the token and returns to Swagger.
    public async Task<IActionResult> LoginCallback()
    {
        string code = Request.Query["code"].ToString();
        string error = Request.Query["error"].ToString();

        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect($"/swagger?onenoteLogin=failed&message={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Microsoft did not send an authorization code.");
        }

        OneNoteTokenDto token = await authService.ExchangeCodeForTokenAsync(code, string.Empty);
        tokenStore.SaveToken(token.AccessToken, token.ExpiresIn);

        return Redirect("/swagger?onenoteLogin=success");
    }

    /// <summary>
    /// Checks whether the API has a temporary OneNote access token.
    /// </summary>
    [HttpGet("token-status")]
    [ProducesResponseType(typeof(OneNoteLoginStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Step 3: checks if the callback already saved a usable access token.
    public ActionResult<OneNoteLoginStatusDto> GetTokenStatus()
    {
        string accessToken = tokenStore.GetAccessToken();

        return Ok(new OneNoteLoginStatusDto
        {
            HasAccessToken = !string.IsNullOrWhiteSpace(accessToken),
            ExpiresAtUtc = tokenStore.GetExpirationDate()
        });
    }

    /// <summary>
    /// Imports OneNote pages into the OneNote staging table.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(OneNoteImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Step 4: reads OneNote pages and saves them into the source staging table.
    public async Task<ActionResult<OneNoteImportResultDto>> Import(OneNoteImportRequestDto request)
    {
        OneNoteImportResultDto result = await importService.ImportAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Processes imported OneNote pages into the knowledge database by mapping id.
    /// </summary>
    [HttpPost("processamento/{mappingId:int}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Step 5: maps imported OneNote rows into Node, Context and Arc tables.
    public async Task<ActionResult<ProcessingResultDto>> Process(int mappingId, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessOneNoteMappingAsync(mappingId, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Processes imported OneNote pages into the knowledge database by table name.
    /// </summary>
    [HttpPost("processamento/tabela/{tableName}")]
    [ProducesResponseType(typeof(ProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
    // Alternative step 5: same processing, but finds the mapping by table name.
    public async Task<ActionResult<ProcessingResultDto>> ProcessByTableName(string tableName, [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 1000)
        {
            return BadRequest(new ErrorDto { Message = "The limit must be between 1 and 1000." });
        }

        ProcessingResultDto? result = await processingService.ProcessOneNoteMappingByTableNameAsync(tableName, limit);

        if (result is null)
        {
            return NotFound(new ErrorDto { Message = "OneNote mapping not found." });
        }

        return Ok(result);
    }
}
