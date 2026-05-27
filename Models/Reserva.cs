namespace api_node_reservas.Models;

public class Reserva
{
    public int Id { get; set; }
    public string? Numero { get; set; }
    public string? Referencia { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? DataPedido { get; set; }
    public DateTime DataActualizacao { get; set; }
    public string NomeUtilizadorConfirmacao { get; set; } = string.Empty;
}
