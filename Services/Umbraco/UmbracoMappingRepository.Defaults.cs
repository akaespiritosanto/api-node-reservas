using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class UmbracoMappingRepository
{
    // Creates beginner-friendly default mappings for Umbraco source tables.
    private List<MappingConfiguration> CreateDefaultMappings()
    {
        return new List<MappingConfiguration>
        {
            CreateCmsContentDefaultMapping()
        };
    }

    private static MappingConfiguration CreateCmsContentDefaultMapping()
    {
        return new MappingConfiguration
        {
            Id = 1,
            TableName = "cmsContent",
            DetectionMethod = "Id",
            IdFieldName = "pk",
            CreationDateFieldName = "",
            UpdateDateFieldName = "",
            Mapping = new KbMapping
            {
                Tabela = "cmsContent",
                Tipo = "CmsContent",
                TipoE = "Content",
                Reference = "nodeId",
                Descricao = "",
                IdInformacao = "pk",
                Contexts = new List<string>(),
                Parent = new List<KbParentMapping>()
            }
        };
    }
}
