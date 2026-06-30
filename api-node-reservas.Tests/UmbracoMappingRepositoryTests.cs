using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace api_node_reservas.Tests;

public class UmbracoMappingRepositoryTests
{
    [Fact]
    // Checks that Umbraco mappings are created in their own JSON file.
    public void Constructor_Creates_Default_Umbraco_Mapping_File()
    {
        string folder = CreateTempFolder();

        try
        {
            UmbracoMappingRepository repository = new UmbracoMappingRepository(new TestEnvironment(folder));

            List<MappingConfiguration> mappings = repository.GetAll();

            Assert.Equal(2, mappings.Count);
            Assert.Equal("cmsDocument", mappings[0].TableName);
            Assert.Equal("text", mappings[0].Mapping.Reference);
            Assert.Equal("cmsContent", mappings[1].TableName);
            Assert.Equal("text", mappings[1].Mapping.Reference);
            Assert.True(File.Exists(Path.Combine(folder, "Data", "Umbraco", "umbraco-mapeamentos.json")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static string CreateTempFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    private class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            WebRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
    }
}
