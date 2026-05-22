namespace api_node_reservas.Dtos;

/*
================================================================================
|                                ErrorDto                                      |
================================================================================
| Este DTO devolve mensagens de erro em JSON.                                   |
|                                                                              |
| Usar sempre o mesmo formato facilita perceber os erros no Swagger ou em outra |
| aplicacao que chame esta API.                                                 |
================================================================================
*/
public class ErrorDto
{
    public string Message { get; set; } = string.Empty;
}
