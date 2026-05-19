using api_node_reservas.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace api_node_reservas.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Returns_400_For_InvalidOperationException()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        ExceptionHandlingMiddleware middleware = new(
            next: _ => throw new InvalidOperationException("Dados invalidos."),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns_500_For_Unexpected_Exception()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        ExceptionHandlingMiddleware middleware = new(
            next: _ => throw new Exception("Erro inesperado."),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}
