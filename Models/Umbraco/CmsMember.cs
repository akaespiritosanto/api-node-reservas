namespace api_node_reservas.Models;

public class CmsMember
{
    public int nodeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string LoginName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
