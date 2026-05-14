using api_node_reservas.Models;
using System.ComponentModel.DataAnnotations;

namespace api_node_reservas.Dtos;

public class MappingConfigurationDto
{
    [Required]
    public string TableName { get; set; } = string.Empty;

    [Required]
    public string DetectionMethod { get; set; } = "Id";

    [Required]
    public string IdFieldName { get; set; } = "id";

    [Required]
    public string CreationDateFieldName { get; set; } = string.Empty;

    [Required]
    public string UpdateDateFieldName { get; set; } = string.Empty;

    public int LastProcessedId { get; set; }
    public DateTime? LastSuccessfulProcessingDate { get; set; }

    [Required]
    public KbMapping Mapping { get; set; } = new();
}
