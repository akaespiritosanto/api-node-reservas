using api_node_reservas.Services;

namespace api_node_reservas.Tests;

public class DotEnvServiceTests
{
    [Fact]
    // Checks that values in a .env file are loaded into environment variables.
    public void Load_Adds_Values_To_Environment()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        string filePath = Path.Combine(folder, ".env");
        File.WriteAllLines(filePath,
        new string[]
        {
            "TEST_API_KEY=abc123",
            "TEST_CONNECTION=\"Server=test;Database=db;\""
        });

        try
        {
            DotEnvService.Load(filePath);

            Assert.Equal("abc123", Environment.GetEnvironmentVariable("TEST_API_KEY"));
            Assert.Equal("Server=test;Database=db;", Environment.GetEnvironmentVariable("TEST_CONNECTION"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_API_KEY", null);
            Environment.SetEnvironmentVariable("TEST_CONNECTION", null);
            Directory.Delete(folder, true);
        }
    }
}
