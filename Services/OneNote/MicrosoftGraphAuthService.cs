using api_node_reservas.Dtos;
using System.Text.Json;

namespace api_node_reservas.Services;

/*
================================================================================
                             Microsoft Graph login
================================================================================
 This service creates the Microsoft login URL and exchanges the returned
 authorization code for an access token. The access token is later used to read
 OneNote pages from Microsoft Graph.
================================================================================
*/
public class MicrosoftGraphAuthService
{
    private readonly HttpClient httpClient;

    public MicrosoftGraphAuthService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    // Builds the URL that the user opens to sign in with Microsoft.
    public OneNoteAuthUrlDto CreateAuthorizationUrl()
    {
        string tenantId = GetEnvironmentValue("AZURE_TENANT_ID", "common");
        string clientId = GetRequiredEnvironmentValue("AZURE_CLIENT_ID");
        string redirectUri = GetRequiredEnvironmentValue("AZURE_REDIRECT_URI");
        string scopes = "openid profile offline_access User.Read Notes.Read";

        string url =
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId)}/oauth2/v2.0/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            "&response_mode=query";

        return new OneNoteAuthUrlDto { AuthorizationUrl = url };
    }

    // Sends the authorization code to Microsoft and receives an access token.
    public async Task<OneNoteTokenDto> ExchangeCodeForTokenAsync(string authorizationCode, string redirectUri)
    {
        string tenantId = GetEnvironmentValue("AZURE_TENANT_ID", "common");
        string clientId = GetRequiredEnvironmentValue("AZURE_CLIENT_ID");
        string clientSecret = GetRequiredEnvironmentValue("AZURE_CLIENT_SECRET");
        string finalRedirectUri = string.IsNullOrWhiteSpace(redirectUri)
            ? GetRequiredEnvironmentValue("AZURE_REDIRECT_URI")
            : redirectUri;

        Dictionary<string, string> formValues = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = authorizationCode,
            ["redirect_uri"] = finalRedirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid profile offline_access User.Read Notes.Read"
        };

        using FormUrlEncodedContent content = new FormUrlEncodedContent(formValues);
        string tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        using HttpResponseMessage response = await httpClient.PostAsync(tokenUrl, content);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Microsoft login failed. Detail: {responseText}");
        }

        using JsonDocument document = JsonDocument.Parse(responseText);
        JsonElement root = document.RootElement;

        return new OneNoteTokenDto
        {
            AccessToken = GetJsonString(root, "access_token"),
            ExpiresIn = GetJsonInt(root, "expires_in")
        };
    }

    private static string GetRequiredEnvironmentValue(string name)
    {
        // These values come from .env, for example AZURE_CLIENT_ID.
        string? value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is missing in the .env file.");
        }

        return value;
    }

    private static string GetEnvironmentValue(string name, string defaultValue)
    {
        // Some values have a safe default for local testing, like tenant "common".
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int GetJsonInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.GetInt32();
        }

        return 0;
    }
}
