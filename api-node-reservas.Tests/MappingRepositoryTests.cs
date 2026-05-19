using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace api_node_reservas.Tests;

public class MappingRepositoryTests
{
    [Fact]
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
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
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
