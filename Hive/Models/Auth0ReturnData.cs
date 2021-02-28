namespace Hive.Models
{
    /// <summary>
    /// The Auth0 return data to return to the client.
    /// <param name="Domain">The domain for Auth0.</param>
    /// <param name="ClientId">The ClientID to use for Auth0 requests.</param>
    /// <param name="Audience">The Audience to use for Auth0 requests.</param>
    /// </summary>
    public record Auth0ReturnData(string Domain, string ClientId, string Audience);

    /// <summary>
    /// The Auth0 data returned from a token request.
    /// <param name="Access_Token">The access token.</param>
    /// <param name="Refresh_Token">The refresh token.</param>
    /// <param name="Id_Token">The ID token.</param>
    /// <param name="Token_Type">The token type. Should always be "Bearer".</param>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "As this is deserialized from Auth0, this matches their schema.")]
    public record Auth0TokenResponse(string Access_Token, string Refresh_Token, string Id_Token, string Token_Type);
}
