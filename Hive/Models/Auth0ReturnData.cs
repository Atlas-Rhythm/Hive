namespace Hive.Models
{
    public record Auth0ReturnData(string Domain, string ClientId, string Audience);
    public record Auth0TokenResponse(string Access_Token, string Refresh_Token, string Id_Token, string Token_Type);
}
