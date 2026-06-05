using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class MappingRepository
{
    /*
    ============================================================================
                              Reservas default mappings
    ============================================================================
     These are the starter mappings created when Data/reservas-mapeamentos.json
     does not exist yet. They are in their own file so the repository CRUD methods
     stay easy to read.
    ============================================================================
    */

    // Creates the starter mappings for Reserva and ProdutoReservado.
    private void CreateDefaultFileIfNeeded()
    {
        if (File.Exists(filePath))
        {
            return;
        }

        List<MappingConfiguration> defaults = new List<MappingConfiguration>
        {
            CreateReservaDefaultMapping(),
            CreateProdutoReservadoDefaultMapping()
        };

        SaveAll(defaults);
    }

    // Creates the default mapping for the Reserva table.
    private static MappingConfiguration CreateReservaDefaultMapping()
    {
        return new MappingConfiguration
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
                Link = "",
                Security = "0",
                UpdateUser = "0",
                DescriptionType = "",
                ContextPar1 = "",
                ContextDescriptionType = "",
                ParentType = "",
                Contexts = new List<string>
                {
                    "estado",
                    "estado_pagamento",
                    "id_canal"
                }
            }
        };
    }

    // Creates the default mapping for the ProdutoReservado table.
    private static MappingConfiguration CreateProdutoReservadoDefaultMapping()
    {
        return new MappingConfiguration
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
                Link = "",
                Security = "0",
                UpdateUser = "0",
                DescriptionType = "",
                ContextPar1 = "",
                ContextDescriptionType = "",
                ParentType = "Reserva",
                Contexts = new List<string>
                {
                    "estado",
                    "quantidade",
                    "id_entidade"
                },
                Parent = new List<string>
                {
                    "id_reserva"
                },
                Relations = new List<KbRelationMapping>
                {
                    new KbRelationMapping
                    {
                        TypeId = "0",
                        Type = "pertence_a_reserva",
                        TargetId = "id_reserva",
                        TargetType = "Reserva"
                    }
                }
            }
        };
    }
}
