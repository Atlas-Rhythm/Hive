using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Text.Json;

namespace Hive.Models
{
    // User would ideally come from the auth server, and be a thin proxy to the appropriate RPC calls
    public class User : IIdentity
    {
        // TODO: this should be from the authentication client library

        // this should have at least:
        // - name
        // - unique id (might be name?)
        // - references to localization objects ala LocalizedModInfo with:
        //   - bio
        // - links to various info about the user
        // - a prfile pic
        // - an extra data object like Mod.AdditionalData

        [Key]
        public string DumbId { get; set; } = null!;

        public string? Username { get; set; }
        public JsonElement AdditionalData { get; set; }

        public string? AuthenticationType { get; set; }

        public bool IsAuthenticated { get; set; }

        public string? Name => Username;
    }
}