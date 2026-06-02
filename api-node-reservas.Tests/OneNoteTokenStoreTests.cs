using api_node_reservas.Services;

namespace api_node_reservas.Tests;

public class OneNoteTokenStoreTests
{
    [Fact]
    // Checks that a valid token can be read after it is saved.
    public void SaveToken_Stores_Token()
    {
        OneNoteTokenStore store = new OneNoteTokenStore();

        store.SaveToken("token-value", 60);

        Assert.Equal("token-value", store.GetAccessToken());
        Assert.NotNull(store.GetExpirationDate());
    }

    [Fact]
    // Checks that expired tokens are cleared and not returned.
    public void GetAccessToken_Clears_Expired_Token()
    {
        OneNoteTokenStore store = new OneNoteTokenStore();

        store.SaveToken("expired-token", -1);

        Assert.Equal(string.Empty, store.GetAccessToken());
        Assert.Null(store.GetExpirationDate());
    }
}
