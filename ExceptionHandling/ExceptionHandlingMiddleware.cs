using api_node_reservas.Dtos;

namespace api_node_reservas.ExceptionHandling;

/*
================================================================================
                              Error handling
================================================================================
 This middleware catches unexpected exceptions and returns a JSON error response
 instead of exposing internal stack traces to the API user.
================================================================================
*/
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Invalid operation while processing the request.");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error while processing the request.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Erro interno no servidor.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ErrorDto { Message = message });
    }
}
