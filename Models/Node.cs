namespace api_node_reservas.Models;

public class Node
{
    public int Id { get; set; }
    public string SourceTable { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string TipoE { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string IdInformacao { get; set; } = string.Empty;
    public string Par1 { get; set; } = string.Empty;
    public string Par2 { get; set; } = string.Empty;
    public string Par3 { get; set; } = string.Empty;
    public string Par4 { get; set; } = string.Empty;
    public string Par5 { get; set; } = string.Empty;
    public string Par6 { get; set; } = string.Empty;
    public string Par7 { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataActualizacao { get; set; }
}
