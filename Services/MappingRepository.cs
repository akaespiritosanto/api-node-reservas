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

    public MappingRepository(IWebHostEnvironment environment)
    {
        string dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);
        filePath = Path.Combine(dataFolder, "mapeamentos.json");
        CreateDefaultFileIfNeeded();
    }

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
                throw new InvalidOperationException($"Ja existe um mapeamento para a tabela '{tableName}'.");
            }
        }
    }

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

    private void SaveAll(List<MappingConfiguration> mappings)
    {
        string json = JsonSerializer.Serialize(mappings, jsonOptions);
        File.WriteAllText(filePath, json);
    }

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
