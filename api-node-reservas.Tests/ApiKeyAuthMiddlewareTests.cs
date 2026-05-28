using api_node_reservas.ApiKeyAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace api_node_reservas.Tests;

public class ApiKeyAuthMiddlewareTests
{
    [Fact]
    // Checks that requests without x-api-key are blocked.
    public async Task InvokeAsync_Returns_401_When_Api_Key_Is_Missing()
    {
        Environment.SetEnvironmentVariable("API_KEY", "test-key");
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        ApiKeyAuthMiddleware middleware = new(
            next: _ => Task.CompletedTask,
            logger: NullLogger<ApiKeyAuthMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    // Checks that requests with the correct x-api-key continue to the next middleware.
    public async Task InvokeAsync_Calls_Next_When_Api_Key_Is_Valid()
    {
        Environment.SetEnvironmentVariable("API_KEY", "test-key");
        DefaultHttpContext context = new();
        context.Request.Headers["x-api-key"] = "test-key";
        bool nextWasCalled = false;

        ApiKeyAuthMiddleware middleware = new(
            next: _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            logger: NullLogger<ApiKeyAuthMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
    }
}
