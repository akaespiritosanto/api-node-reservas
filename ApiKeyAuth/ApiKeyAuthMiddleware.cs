using api_node_reservas.Dtos;

namespace api_node_reservas.ApiKeyAuth;

/*
================================================================================
                                API key security
================================================================================
 This middleware blocks requests that do not send the expected x-api-key header.
 The expected key comes from the API_KEY environment variable.
================================================================================
*/
public class ApiKeyAuthMiddleware
{
    private const string HeaderName = "x-api-key";
    private readonly RequestDelegate next;
    private readonly ILogger<ApiKeyAuthMiddleware> logger;

    // Receives the next middleware so this class can continue the request when the key is valid.
    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    // Checks each request for the x-api-key header before allowing it to continue.
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsSwaggerRequest(context))
        {
            await next(context);
            return;
        }

        string? configuredApiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            logger.LogError("API_KEY is not configured.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = "API_KEY is not configured in the .env file." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var receivedApiKey) || receivedApiKey != configuredApiKey)
        {
            logger.LogWarning("Request blocked because the API key is missing or invalid.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = "x-api-key is invalid or missing." });
            return;
        }

        await next(context);
    }

    // Allows Swagger files to load without an API key, so the documentation page opens normally.
    private static bool IsSwaggerRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/swagger");
    }
}
