using api_node_reservas.Dtos;
using api_node_reservas.Models;
using System.Text.Json;

namespace api_node_reservas.Services;

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
        return JsonSerializer.Deserialize<List<MappingConfiguration>>(json, jsonOptions) ?? [];
    }

    public MappingConfiguration? GetById(int id)
    {
        return GetAll().FirstOrDefault(mapping => mapping.Id == id);
    }

    public MappingConfiguration Create(MappingConfigurationDto dto)
    {
        List<MappingConfiguration> mappings = GetAll();
        int nextId = mappings.Count == 0 ? 1 : mappings.Max(mapping => mapping.Id) + 1;

        MappingConfiguration mapping = ConvertDtoToModel(dto);
        mapping.Id = nextId;
        mappings.Add(mapping);
        SaveAll(mappings);

        return mapping;
    }

    public bool Update(int id, MappingConfigurationDto dto)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? existingMapping = mappings.FirstOrDefault(mapping => mapping.Id == id);

        if (existingMapping is null)
        {
            return false;
        }

        MappingConfiguration updatedMapping = ConvertDtoToModel(dto);
        updatedMapping.Id = id;
        int index = mappings.IndexOf(existingMapping);
        mappings[index] = updatedMapping;
        SaveAll(mappings);

        return true;
    }

    public bool Delete(int id)
    {
        List<MappingConfiguration> mappings = GetAll();
        MappingConfiguration? mapping = mappings.FirstOrDefault(mapping => mapping.Id == id);

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
        MappingConfiguration? mapping = mappings.FirstOrDefault(mapping => mapping.Id == id);

        if (mapping is null)
        {
            return;
        }

        mapping.LastProcessedId = lastProcessedId;
        mapping.LastSuccessfulProcessingDate = processingDate;
        SaveAll(mappings);
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

        List<MappingConfiguration> defaults =
        [
            new()
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
                    Descricao = "observacoes",
                    IdInformacao = "id",
                    Par1 = "numero",
                    Par2 = "referencia",
                    Par3 = "nome_utilizador_confirmacao",
                    Contexts =
                    [
                        "estado",
                        "estado_pagamento",
                        "id_canal"
                    ]
                }
            },
            new()
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
                    Descricao = "nome_produto",
                    IdInformacao = "id",
                    Par1 = "id_reserva",
                    Par2 = "id_produto",
                    Par3 = "referencia",
                    Par4 = "DataInicio",
                    Par5 = "DataFim",
                    Contexts =
                    [
                        "estado",
                        "quantidade",
                        "id_entidade"
                    ],
                    Relations =
                    [
                        new KbRelationMapping
                        {
                            Type = "pertence_a_reserva",
                            TargetId = "id_reserva"
                        }
                    ]
                }
            }
        ];

        SaveAll(defaults);
    }
}
