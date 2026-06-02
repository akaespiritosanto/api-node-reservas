using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                          OneNote mapping repository
================================================================================
 This repository reads and writes Data/onenote-mapeamentos.json.
 It is separate from MappingRepository so OneNote mappings do not mix with the
 Reservas mappings stored in Data/mapeamentos.json.
================================================================================
*/
public class OneNoteMappingRepository
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public OneNoteMappingRepository(IWebHostEnvironment environment)
    {
        // The JSON file lives inside the project Data folder.
        string dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);

        filePath = Path.Combine(dataFolder, "onenote-mapeamentos.json");

        if (!File.Exists(filePath))
        {
            // Create a beginner-friendly default mapping the first time the API runs.
            SaveAll(CreateDefaultMappings());
        }
    }

    // Reads all OneNote mapping configurations from the JSON file.
    public List<MappingConfiguration> GetAll()
    {
        string json = File.ReadAllText(filePath);
        List<MappingConfiguration>? mappings = JsonSerializer.Deserialize<List<MappingConfiguration>>(json, jsonOptions);
        return mappings ?? new List<MappingConfiguration>();
    }

    // Finds one OneNote mapping by its numeric id.
    public MappingConfiguration? GetById(int id)
    {
        foreach (MappingConfiguration mapping in GetAll())
        {
            if (mapping.Id == id)
            {
                return mapping;
            }
        }

        return null;
    }

    // Finds one OneNote mapping by source table name.
    public MappingConfiguration? GetByTableName(string tableName)
    {
        foreach (MappingConfiguration mapping in GetAll())
        {
            if (mapping.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    // Saves the checkpoint after processing imported OneNote pages.
    public void UpdateProcessingState(int mappingId, int lastProcessedId, DateTime processingDate)
    {
        List<MappingConfiguration> mappings = GetAll();

        foreach (MappingConfiguration mapping in mappings)
        {
            if (mapping.Id == mappingId)
            {
                mapping.LastProcessedId = lastProcessedId;
                mapping.LastSuccessfulProcessingDate = processingDate;
                SaveAll(mappings);
                return;
            }
        }
    }

    // Writes every mapping back to Data/onenote-mapeamentos.json.
    private void SaveAll(List<MappingConfiguration> mappings)
    {
        string json = JsonSerializer.Serialize(mappings, jsonOptions);
        File.WriteAllText(filePath, json);
    }

    // Creates the default mapping from OneNotePageImport to the knowledge database.
    private static List<MappingConfiguration> CreateDefaultMappings()
    {
        return new List<MappingConfiguration>
        {
            new MappingConfiguration
            {
                Id = 1,
                TableName = "OneNotePageImport",
                DetectionMethod = "CreationDate",
                IdFieldName = "id",
                CreationDateFieldName = "createdDateTime",
                UpdateDateFieldName = "lastModifiedDateTime",
                Mapping = new KbMapping
                {
                    Tabela = "OneNotePageImport",
                    Tipo = "OneNotePage",
                    TipoE = "Note",
                    Reference = "graphPageId",
                    Descricao = "contentText",
                    IdInformacao = "id",
                    Par1 = "pageTitle",
                    Par2 = "notebookName",
                    Par3 = "sectionName",
                    Par4 = "webUrl",
                    Contexts = new List<string>
                    {
                        "userId",
                        "notebookName",
                        "sectionName"
                    }
                }
            }
        };
    }
}
