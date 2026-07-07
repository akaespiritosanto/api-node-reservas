using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace api_node_reservas.Swagger;

/*
================================================================================
                              Swagger endpoint order
================================================================================
 This helper keeps Swagger organized in the order a beginner usually follows:
 1. Check or create mappings.
 2. Process Reservas data.
 3. Check or create OneNote mappings.
 4. Login, import and process OneNote data.
 5. Synchronize OneNote and Node data.
================================================================================
*/
public static class SwaggerEndpointOrder
{
    // Builds the sortable text used by Swagger to order endpoints.
    public static string GetActionOrder(ApiDescription apiDescription)
    {
        string? controller = apiDescription.ActionDescriptor.RouteValues["controller"];
        string endpointPath = apiDescription.RelativePath ?? string.Empty;
        int group = GetGroupOrder(controller, endpointPath);
        int endpoint = GetEndpointOrder(controller, endpointPath, apiDescription.HttpMethod);

        return $"{group:000}_{endpoint:000}_{controller}";
    }

    // Returns the section name shown in Swagger.
    public static string GetTag(ApiDescription apiDescription)
    {
        string? controller = apiDescription.ActionDescriptor.RouteValues["controller"];
        string endpointPath = apiDescription.RelativePath ?? string.Empty;

        if (controller == "Mapeamentos_Reservas")
        {
            return "01 - Reservas - Mapeamentos";
        }

        if (controller == "Processamento_Reservas")
        {
            return "02 - Reservas - Processamento";
        }

        if (controller == "Mapeamentos_OneNote")
        {
            return "03 - OneNote - Mapeamentos";
        }

        if (controller == "Processamento_OneNote" && IsOneNoteSyncPath(endpointPath))
        {
            return "05 - OneNote - Sincronizacao";
        }

        if (controller == "Processamento_OneNote")
        {
            return "04 - OneNote - Processamento";
        }

        if (controller == "Mapeamentos_Umbraco")
        {
            return "06 - Umbraco - Mapeamentos";
        }

        if (controller == "Processamento_Umbraco")
        {
            return "07 - Umbraco - Processamento";
        }

        return controller ?? "Outros";
    }

    // Gives each Swagger section its place in the page.
    private static int GetGroupOrder(string? controller, string path)
    {
        if (controller == "Mapeamentos_Reservas")
        {
            return 10;
        }

        if (controller == "Processamento_Reservas")
        {
            return 20;
        }

        if (controller == "Mapeamentos_OneNote")
        {
            return 30;
        }

        if (controller == "Processamento_OneNote")
        {
            if (IsOneNoteSyncPath(path))
            {
                return 50;
            }

            return 40;
        }

        if (controller == "Mapeamentos_Umbraco")
        {
            return 60;
        }

        if (controller == "Processamento_Umbraco")
        {
            return 70;
        }

        return 100;
    }

    // Orders endpoints inside each Swagger section.
    private static int GetEndpointOrder(string? controller, string? path, string? httpMethod)
    {
        string endpointPath = path ?? string.Empty;
        string method = httpMethod ?? string.Empty;

        if (controller == "Mapeamentos_Reservas" || controller == "Mapeamentos_OneNote")
        {
            return GetCrudOrder(endpointPath, method);
        }

        if (controller == "Processamento_Reservas")
        {
            return GetReservasProcessingOrder(endpointPath);
        }

        if (controller == "Processamento_OneNote")
        {
            return GetOneNoteProcessingOrder(endpointPath);
        }

        return 100;
    }

    // Orders CRUD endpoints as list, get, get by table, create, update and delete.
    private static int GetCrudOrder(string path, string method)
    {
        if (path.Contains("tabela", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (path.EndsWith("}", StringComparison.OrdinalIgnoreCase) && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        return 10;
    }

    // Reservas should normally process Reserva before ProdutoReservado.
    private static int GetReservasProcessingOrder(string path)
    {
        if (path.Contains("tabela/Reserva", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (path.Contains("tabela/ProdutoReservado", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (path.Contains("tabela", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        return 40;
    }

    // OneNote should login first, then import, then process.
    private static int GetOneNoteProcessingOrder(string path)
    {
        if (IsOneNoteSyncPath(path))
        {
            return GetOneNoteSyncOrder(path);
        }

        if (path.EndsWith("login-url", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (path.EndsWith("callback", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (path.EndsWith("token-status", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (path.EndsWith("import", StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (path.Contains("processamento/tabela", StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        if (path.Contains("processamento", StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        return 100;
    }

    // OneNote synchronization has its own Swagger section.
    private static bool IsOneNoteSyncPath(string path)
    {
        return path.Contains("sync/", StringComparison.OrdinalIgnoreCase);
    }

    // Sync one Node first, then the batch sync endpoint.
    private static int GetOneNoteSyncOrder(string path)
    {
        if (path.Contains("sync/node/", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (path.EndsWith("sync/nodes", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        return 30;
    }
}
