using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hive.Models
{
    /// <summary>
    /// A moderately thin proxy to the auth server, holds some information that will probably be useful.
    /// </summary>
    public class User
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

        /// <summary>
        /// The username of the user.
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// An alternative ID of the user.
        /// In Auth0's case, this would be the auth0 unique ID, which would then be mappable to this particular username/user structure.
        /// </summary>
        [Key]
        public string AlternativeId { get; set; } = null!;

        /// <summary>
        /// The additional data attached to the user object.
        /// This is a dictionary as it holds each of the exact identities from Auth0 or any other authentication platform.
        /// Each of these can map to an arbitrary JSON object, so we map it like such.
        /// Ideally, a conversion step would take place in order to convert the values of this dictionary into something more legible, perhaps some form of interface providable to plugins.
        /// </summary>
        [Column(TypeName = "jsonb")]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "We want to set it explicitly to our data from our Auth0 instance, and also allow plugins to do the same.")]
        public Dictionary<string, JsonElement> AdditionalData { get; set; } = new();
    }
}
