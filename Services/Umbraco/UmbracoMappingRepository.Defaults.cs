using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class UmbracoMappingRepository
{
    // Creates beginner-friendly default mappings for Umbraco source tables.
    private List<MappingConfiguration> CreateDefaultMappings()
    {
        return new List<MappingConfiguration>
        {
            CreateCmsDocumentDefaultMapping(),
            CreateCmsContentDefaultMapping()
        };
    }

    private static MappingConfiguration CreateCmsDocumentDefaultMapping()
    {
        return new MappingConfiguration
        {
            Id = 1,
            TableName = "cmsDocument",
            DetectionMethod = "Id",
            IdFieldName = "nodeId",
            CreationDateFieldName = "",
            UpdateDateFieldName = "updateDate",
            Mapping = new KbMapping
            {
                Tabela = "cmsDocument",
                Tipo = "contentType",
                TipoE = "Document",
                Reference = "text",
                Descricao = "",
                IdInformacao = "nodeId",
                Contexts = new List<string>(),
                Parent = new List<KbParentMapping>
                {
                    new KbParentMapping
                    {
                        FieldName = "parentID"
                    }
                }
            }
        };
    }

    private static MappingConfiguration CreateCmsContentDefaultMapping()
    {
        return new MappingConfiguration
        {
            Id = 2,
            TableName = "cmsContent",
            DetectionMethod = "Id",
            IdFieldName = "nodeId",
            CreationDateFieldName = "",
            UpdateDateFieldName = "",
            Mapping = new KbMapping
            {
                Tabela = "cmsContent",
                Tipo = "contentType",
                TipoE = "Content",
                Reference = "text",
                Descricao = "",
                IdInformacao = "nodeId",
                Contexts = new List<string>(),
                Parent = new List<KbParentMapping>
                {
                    new KbParentMapping
                    {
                        FieldName = "parentID"
                    }
                }
            }
        };
    }
}
