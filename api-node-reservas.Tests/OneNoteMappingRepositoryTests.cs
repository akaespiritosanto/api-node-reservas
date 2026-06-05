using api_node_reservas.Dtos;
using api_node_reservas.Models;
using api_node_reservas.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace api_node_reservas.Tests;

public class OneNoteMappingRepositoryTests
{
    [Fact]
    // Checks that OneNote mappings are created in their own JSON file.
    public void Constructor_Creates_Default_OneNote_Mapping_File()
    {
        string folder = CreateTempFolder();

        try
        {
            OneNoteMappingRepository repository = new OneNoteMappingRepository(new TestEnvironment(folder));

            List<MappingConfiguration> mappings = repository.GetAll();

            Assert.Single(mappings);
            Assert.Equal("OneNotePageImport", mappings[0].TableName);
            Assert.Equal("pageTitle", mappings[0].Mapping.Reference);
            Assert.Equal("contentText", mappings[0].Mapping.Descricao);
            Assert.Equal("notebookName", mappings[0].Mapping.Par1);
            Assert.Equal("sectionName", mappings[0].Mapping.Par2);
            Assert.Equal("webUrl", mappings[0].Mapping.Link);
            Assert.Equal("graphPageId", mappings[0].Mapping.ExternalId);
            Assert.True(File.Exists(Path.Combine(folder, "Data", "onenote-mapeamentos.json")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    // Checks that the OneNote checkpoint is saved in onenote-mapeamentos.json.
    public void UpdateProcessingState_Saves_OneNote_Checkpoint()
    {
        string folder = CreateTempFolder();

        try
        {
            OneNoteMappingRepository repository = new OneNoteMappingRepository(new TestEnvironment(folder));
            DateTime processingDate = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc);

            repository.UpdateProcessingState(1, 123, processingDate);

            MappingConfiguration? mapping = repository.GetById(1);

            Assert.NotNull(mapping);
            Assert.Equal(123, mapping.LastProcessedId);
            Assert.Equal(processingDate, mapping.LastSuccessfulProcessingDate);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    // Checks that OneNote mappings can be created, changed and deleted like Reservas mappings.
    public void Create_Update_And_Delete_OneNote_Mapping()
    {
        string folder = CreateTempFolder();

        try
        {
            OneNoteMappingRepository repository = new OneNoteMappingRepository(new TestEnvironment(folder));

            MappingConfiguration created = repository.Create(CreateDto("OneNoteCustomTable"));

            Assert.Equal("OneNoteCustomTable", created.TableName);
            Assert.NotNull(repository.GetById(created.Id));

            bool updated = repository.Update(created.Id, CreateDto("OneNoteCustomTableUpdated"));
            MappingConfiguration? saved = repository.GetById(created.Id);

            Assert.True(updated);
            Assert.NotNull(saved);
            Assert.Equal("OneNoteCustomTableUpdated", saved.TableName);

            bool deleted = repository.Delete(created.Id);

            Assert.True(deleted);
            Assert.Null(repository.GetById(created.Id));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // Creates a small valid mapping request for the OneNote repository tests.
    private static MappingConfigurationDto CreateDto(string tableName)
    {
        return new MappingConfigurationDto
        {
            TableName = tableName,
            DetectionMethod = "Id",
            IdFieldName = "id",
            CreationDateFieldName = "createdDateTime",
            UpdateDateFieldName = "lastModifiedDateTime",
            Mapping = new KbMapping
            {
                Tabela = tableName,
                Tipo = "OneNotePage",
                TipoE = "Note",
                Reference = "graphPageId",
                Descricao = "contentText",
                IdInformacao = "id"
            }
        };
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
