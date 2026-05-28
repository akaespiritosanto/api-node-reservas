using api_node_reservas.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace api_node_reservas.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    // Checks that expected business errors become HTTP 400 responses.
    public async Task InvokeAsync_Returns_400_For_InvalidOperationException()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        ExceptionHandlingMiddleware middleware = new(
            next: _ => throw new InvalidOperationException("Invalid data."),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    // Checks that unexpected errors become HTTP 500 responses.
    public async Task InvokeAsync_Returns_500_For_Unexpected_Exception()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        ExceptionHandlingMiddleware middleware = new(
            next: _ => throw new Exception("Unexpected error."),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}
