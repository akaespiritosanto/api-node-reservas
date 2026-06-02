namespace api_node_reservas.Services;

/*
================================================================================
                           Temporary OneNote token store
================================================================================
 This keeps the Microsoft access token in memory after the user logs in.
 It is intentionally simple for local testing:
 - restarting the API clears the token;
 - no token is written to disk;
 - expired tokens are removed automatically.
================================================================================
*/
public class OneNoteTokenStore
{
    private string accessToken = string.Empty;
    private DateTime? expiresAtUtc;

    public void SaveToken(string newAccessToken, int expiresInSeconds)
    {
        // Microsoft tells us how many seconds the token is valid.
        accessToken = newAccessToken;
        expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }

    public string GetAccessToken()
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return string.Empty;
        }

        if (expiresAtUtc is not null && expiresAtUtc <= DateTime.UtcNow)
        {
            // Never return an expired token.
            Clear();
            return string.Empty;
        }

        return accessToken;
    }

    public DateTime? GetExpirationDate()
    {
        return expiresAtUtc;
    }

    public void Clear()
    {
        accessToken = string.Empty;
        expiresAtUtc = null;
    }
}
