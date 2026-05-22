using api_node_reservas.Dtos;

namespace api_node_reservas.ExceptionHandling;

/*
================================================================================
|                        ExceptionHandlingMiddleware                           |
================================================================================
| Este middleware apanha erros que acontecem durante um pedido HTTP.            |
|                                                                              |
| Assim a API responde sempre com JSON em vez de mostrar mensagens tecnicas     |
| dificeis de perceber para quem esta a consumir a API.                         |
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error while processing the request.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorDto { Message = "Erro interno no servidor." });
        }
    }
}
