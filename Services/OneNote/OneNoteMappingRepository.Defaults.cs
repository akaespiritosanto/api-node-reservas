using api_node_reservas.Models;

namespace api_node_reservas.Services;

public partial class OneNoteMappingRepository
{
    /*
    ============================================================================
                              OneNote default mappings
    ============================================================================
     This file contains only the starter OneNote mapping. Keeping it separate
     makes OneNoteMappingRepository.cs easier to read.
    ============================================================================
    */

    // Creates the default mapping from OneNotePageImport to the knowledge database.
    private static List<MappingConfiguration> CreateDefaultMappings()
    {
        return new List<MappingConfiguration>
        {
            new MappingConfiguration
            {
                Id = 1,
                TableName = "OneNotePageImport",
                DetectionMethod = "CreationDate",
                IdFieldName = "id",
                CreationDateFieldName = "createdDateTime",
                UpdateDateFieldName = "lastModifiedDateTime",
                Mapping = new KbMapping
                {
                    Tabela = "OneNotePageImport",
                    Tipo = "OneNotePage",
                    TipoE = "Note",
                    Reference = "graphPageId",
                    Descricao = "contentText",
                    IdInformacao = "id",
                    Par1 = "pageTitle",
                    Par2 = "notebookName",
                    Par3 = "sectionName",
                    Par4 = "webUrl",
                    Contexts = new List<string>
                    {
                        "userId",
                        "notebookName",
                        "sectionName"
                    }
                }
            }
        };
    }
}
