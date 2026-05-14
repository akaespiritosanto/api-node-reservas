using api_node_reservas.Dtos;

namespace api_node_reservas.Middleware;

public class ApiKeyMiddleware
{
    private const string HeaderName = "x-api-key";
    private readonly RequestDelegate next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

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
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = "API_KEY nao foi configurada no ficheiro .env." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var receivedApiKey) || receivedApiKey != configuredApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = "x-api-key invalida ou em falta." });
            return;
        }

        await next(context);
    }

    private static bool IsSwaggerRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/swagger");
    }
}
