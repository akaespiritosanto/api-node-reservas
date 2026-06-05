using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

public partial class OneNoteMappingRepository
{
    /*
    ============================================================================
                              OneNote repository helpers
    ============================================================================
     These helper methods keep OneNoteMappingRepository.cs focused on the public
     CRUD operations that Swagger calls.
    ============================================================================
    */

    // Writes every mapping back to Data/onenote-mapeamentos.json.
    private void SaveAll(List<MappingConfiguration> mappings)
    {
        string json = JsonSerializer.Serialize(mappings, jsonOptions);
        File.WriteAllText(filePath, json);
    }

    // Prevents two OneNote mappings from using the same source table name.
    private static void ValidateTableNameIsAvailable(List<MappingConfiguration> mappings, string tableName, int? currentMappingId)
    {
        foreach (MappingConfiguration mapping in mappings)
        {
            if (currentMappingId is not null && mapping.Id == currentMappingId)
            {
                continue;
            }

            if (mapping.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"A OneNote mapping already exists for table '{tableName}'.");
            }
        }
    }

    // Searches a list of OneNote mappings by id.
    private static MappingConfiguration? FindById(List<MappingConfiguration> mappings, int id)
    {
        foreach (MappingConfiguration mapping in mappings)
        {
            if (mapping.Id == id)
            {
                return mapping;
            }
        }

        return null;
    }

    // Finds the next id by looking for the biggest current id.
    private static int GetNextId(List<MappingConfiguration> mappings)
    {
        int biggestId = 0;

        foreach (MappingConfiguration mapping in mappings)
        {
            if (mapping.Id > biggestId)
            {
                biggestId = mapping.Id;
            }
        }

        return biggestId + 1;
    }

    // Converts the API request DTO into the model saved in the JSON file.
    private static MappingConfiguration ConvertDtoToModel(MappingConfigurationDto dto)
    {
        return new MappingConfiguration
        {
            TableName = dto.TableName,
            DetectionMethod = dto.DetectionMethod,
            IdFieldName = dto.IdFieldName,
            CreationDateFieldName = dto.CreationDateFieldName,
            UpdateDateFieldName = dto.UpdateDateFieldName,
            LastProcessedId = dto.LastProcessedId,
            LastSuccessfulProcessingDate = dto.LastSuccessfulProcessingDate,
            Mapping = dto.Mapping
        };
    }
}
