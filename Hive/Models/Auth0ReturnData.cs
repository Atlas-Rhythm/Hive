using System.Text.Json.Serialization;

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
    /// </summary>
    public record Auth0TokenResponse
    {
        /// <summary>
        /// The access token.
        /// </summary>
        [JsonPropertyName("Access_Token")]
        public string AccessToken { get; set; }
        /// <summary>
        /// The refresh token.
        /// </summary>
        [JsonPropertyName("Refresh_Token")]
        public string RefreshToken { get; set; }
        /// <summary>
        /// The ID token.
        /// </summary>
        [JsonPropertyName("Id_Token")]
        public string IdToken { get; set; }
        /// <summary>
        /// The token type. Should be "Bearer".
        /// </summary>
        [JsonPropertyName("Token_Type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Create an Auth0Token Response
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="refreshToken"></param>
        /// <param name="idToken"></param>
        /// <param name="tokenType"></param>
        public Auth0TokenResponse(string accessToken, string refreshToken, string idToken, string tokenType)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            IdToken = idToken;
            TokenType = tokenType;
        }
    }
}
