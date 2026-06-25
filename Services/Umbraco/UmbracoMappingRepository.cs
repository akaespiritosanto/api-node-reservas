using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                          Umbraco mapping repository
================================================================================
 This repository reads and writes Data/umbraco-mapeamentos.json. It follows
 the same structure as the OneNote repository but keeps its mappings in a
 separate file so they do not mix with Reservas mappings.
================================================================================
*/
public partial class UmbracoMappingRepository
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public UmbracoMappingRepository(IWebHostEnvironment environment)
    {
        string dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);

        filePath = Path.Combine(dataFolder, "umbraco-mapeamentos.json");

        if (!File.Exists(filePath))
        {
            SaveAll(CreateDefaultMappings());
        }
    }

    public List<MappingConfiguration> GetAll()
    {
        string json = File.ReadAllText(filePath);
        List<MappingConfiguration>? mappings = JsonSerializer.Deserialize<List<MappingConfiguration>>(json, jsonOptions);
        return mappings ?? new List<MappingConfiguration>();
    }

    public MappingConfiguration? GetById(int id)
    {
        List<MappingConfiguration> mappings = GetAll();
        return FindById(mappings, id);
    }

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
