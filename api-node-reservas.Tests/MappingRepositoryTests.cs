using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace api_node_reservas.Tests;

public class MappingRepositoryTests
{
    [Fact]
    // Checks that a new repository creates the default mapping file automatically.
    // Beginner note: the repository stores mapping configurations in a JSON
    // file. This test ensures the defaults (Reserva, ProdutoReservado) exist
    // when a repository is created for the first time.
    public void Constructor_Creates_Default_Mappings_File()
    {
        string folder = CreateTempFolder();

        try
        {
            MappingRepository repository = new(new TestEnvironment(folder));

            List<MappingConfiguration> mappings = repository.GetAll();

            Assert.True(mappings.Count >= 2);
            Assert.Contains(mappings, mapping => mapping.TableName == "Reserva");
            Assert.Contains(mappings, mapping => mapping.TableName == "ProdutoReservado");
            Assert.True(File.Exists(Path.Combine(folder, "Data", "reservas-mapeamentos.json")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    // Checks that Create adds a new mapping and saves it to the JSON file.
    // Beginner note: creating a mapping means defining how a source table
    // becomes knowledge nodes. The test verifies the created mapping is
    // persisted and retrievable.
    public void Create_Adds_New_Mapping()
    {
        string folder = CreateTempFolder();

        try
        {
            MappingRepository repository = new(new TestEnvironment(folder));

            MappingConfiguration created = repository.Create(new MappingConfigurationDto
            {
                TableName = "Teste",
                DetectionMethod = "Id",
                IdFieldName = "id",
                CreationDateFieldName = "data_criacao",
                UpdateDateFieldName = "data_actualizacao",
                Mapping = new KbMapping
                {
                    Tabela = "Teste",
                    Tipo = "Teste",
                    TipoE = "Teste",
                    Descricao = "descricao",
                    IdInformacao = "id"
                }
            });

            MappingConfiguration? saved = repository.GetById(created.Id);

            Assert.NotNull(saved);
            Assert.Equal("Teste", saved.TableName);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // Creates a temporary folder so each test can use an isolated mapping file.
    private static string CreateTempFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    private class TestEnvironment : IWebHostEnvironment
    {
        // Creates a minimal web host environment for tests that need ContentRootPath.
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
