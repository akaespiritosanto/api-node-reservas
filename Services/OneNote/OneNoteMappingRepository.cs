using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                          OneNote mapping repository
================================================================================
 This repository reads and writes Data/onenote-mapeamentos.json.
 It is separate from MappingRepository so OneNote mappings do not mix with the
 Reservas mappings stored in Data/reservas-mapeamentos.json.
================================================================================
*/
public partial class OneNoteMappingRepository
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
        List<MappingConfiguration> mappings = GetAll();
        return FindById(mappings, id);
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

    // Creates a new OneNote mapping, gives it the next id, and saves it to the file.
    public MappingConfiguration Create(MappingConfigurationDto dto)
    {
        List<MappingConfiguration> mappings = GetAll();
        int nextId = GetNextId(mappings);

        ValidateTableNameIsAvailable(mappings, dto.TableName, null);

        MappingConfiguration mapping = ConvertDtoToModel(dto);
        mapping.Id = nextId;
        mappings.Add(mapping);
        SaveAll(mappings);

        return mapping;
    }

    // Replaces an existing OneNote mapping with new values.
    public bool Update(int id, MappingConfigurationDto dto)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? existingMapping = FindById(mappings, id);

        if (existingMapping is null)
        {
            return false;
        }

        MappingConfiguration updatedMapping = ConvertDtoToModel(dto);
        updatedMapping.Id = id;
        ValidateTableNameIsAvailable(mappings, dto.TableName, id);

        int index = mappings.IndexOf(existingMapping);
        mappings[index] = updatedMapping;
        SaveAll(mappings);

        return true;
    }

    // Removes one OneNote mapping from the file.
    public bool Delete(int id)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? mapping = FindById(mappings, id);

        if (mapping is null)
        {
            return false;
        }

        mappings.Remove(mapping);
        SaveAll(mappings);
        return true;
    }

    // Saves the checkpoint after processing imported OneNote pages.
    public void UpdateProcessingState(int mappingId, int lastProcessedId, DateTime processingDate)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? mapping = FindById(mappings, mappingId);

        if (mapping is null)
        {
            return;
        }

        mapping.LastProcessedId = lastProcessedId;
        mapping.LastSuccessfulProcessingDate = processingDate;
        SaveAll(mappings);
    }
}
