using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                              Mapping repository
================================================================================
 This class reads and writes Data/mapeamentos.json. The mapping file tells the
 processing service which source table columns become Node, Context and Arc data.
================================================================================
*/
public class MappingRepository
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    // Builds the path to Data/mapeamentos.json and creates the default file if it does not exist.
    public MappingRepository(IWebHostEnvironment environment)
    {
        string dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);
        filePath = Path.Combine(dataFolder, "mapeamentos.json");
        CreateDefaultFileIfNeeded();
    }

    // Reads all mapping configurations from Data/mapeamentos.json.
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

        foreach (MappingConfiguration mapping in mappings)
        {
            if (mapping.Id == id)
            {
                return mapping;
            }
        }

        return null;
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

    // Prevents two mappings from using the same source table name.
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
                throw new InvalidOperationException($"A mapping already exists for table '{tableName}'.");
            }
        }
    }

    // Searches a list of mappings by id.
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

    // Writes all mappings back to Data/mapeamentos.json.
    private void SaveAll(List<MappingConfiguration> mappings)
    {
        string json = JsonSerializer.Serialize(mappings, jsonOptions);
        File.WriteAllText(filePath, json);
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

    // Creates the starter mappings for Reserva and ProdutoReservado.
    private void CreateDefaultFileIfNeeded()
    {
        if (File.Exists(filePath))
        {
            return;
        }

        List<MappingConfiguration> defaults = new List<MappingConfiguration>
        {
            new MappingConfiguration
            {
                Id = 1,
                TableName = "Reserva",
                DetectionMethod = "Id",
                IdFieldName = "id",
                CreationDateFieldName = "data_pedido",
                UpdateDateFieldName = "data_actualizacao",
                Mapping = new KbMapping
                {
                    Tabela = "Reserva",
                    Tipo = "Reserva",
                    TipoE = "Reserva",
                    Reference = "referencia",
                    Descricao = "observacoes",
                    IdInformacao = "id",
                    Par1 = "numero",
                    Par2 = "referencia",
                    Par3 = "nome_utilizador_confirmacao",
                    Contexts = new List<string>
                    {
                        "estado",
                        "estado_pagamento",
                        "id_canal"
                    }
                }
            },
            new MappingConfiguration
            {
                Id = 2,
                TableName = "ProdutoReservado",
                DetectionMethod = "Id",
                IdFieldName = "id",
                CreationDateFieldName = "data_criacao",
                UpdateDateFieldName = "data_actualizacao",
                Mapping = new KbMapping
                {
                    Tabela = "ProdutoReservado",
                    Tipo = "ProdutoReservado",
                    TipoE = "Produto",
                    Reference = "referencia",
                    Descricao = "nome_produto",
                    IdInformacao = "id",
                    Par1 = "id_reserva",
                    Par2 = "id_produto",
                    Par3 = "referencia",
                    Par4 = "DataInicio",
                    Par5 = "DataFim",
                    Contexts = new List<string>
                    {
                        "estado",
                        "quantidade",
                        "id_entidade"
                    },
                    Relations = new List<KbRelationMapping>
                    {
                        new KbRelationMapping
                        {
                            Type = "pertence_a_reserva",
                            TargetId = "id_reserva",
                            TargetType = "Reserva"
                        }
                    }
                }
            }
        };

        SaveAll(defaults);
    }
}
