using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                              Mapping repository
================================================================================
 This class reads and writes Data/reservas-mapeamentos.json. The mapping file
 tells the processing service which source table columns become Node, Context
 and Arc data.
================================================================================
*/
public partial class MappingRepository
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    // Builds the path to Data/reservas-mapeamentos.json and creates the default file if it does not exist.
    public MappingRepository(IWebHostEnvironment environment)
    {
        string dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);
        filePath = Path.Combine(dataFolder, "reservas-mapeamentos.json");
        CreateDefaultFileIfNeeded();
    }

    // Reads all mapping configurations from Data/reservas-mapeamentos.json.
    public List<MappingConfiguration> GetAll()
    {
        string json = File.ReadAllText(filePath);
        List<MappingConfiguration>? mappings = JsonSerializer.Deserialize<List<MappingConfiguration>>(json, jsonOptions);

        if (mappings is null)
        {
            return new List<MappingConfiguration>();
        }

        return mappings;
    }

    // Finds one mapping by its numeric id.
    public MappingConfiguration? GetById(int id)
    {
        List<MappingConfiguration> mappings = GetAll();
        return FindById(mappings, id);
    }

    // Finds one mapping by the source table name, for example Reserva.
    public MappingConfiguration? GetByTableName(string tableName)
    {
        List<MappingConfiguration> mappings = GetAll();

        foreach (MappingConfiguration mapping in mappings)
        {
            if (mapping.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    // Creates a new mapping, gives it the next id, and saves it to the file.
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

    // Replaces an existing mapping with new values.
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

    // Removes one mapping from the file.
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

    // Saves the last processed id and date after processing finishes.
    public void UpdateProcessingState(int id, int lastProcessedId, DateTime processingDate)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? mapping = FindById(mappings, id);

        if (mapping is null)
        {
            return;
        }

        mapping.LastProcessedId = lastProcessedId;
        mapping.LastSuccessfulProcessingDate = processingDate;
        SaveAll(mappings);
    }
}
