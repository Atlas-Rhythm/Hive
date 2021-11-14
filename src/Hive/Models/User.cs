using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Hive.Controllers;
using Microsoft.EntityFrameworkCore;

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
        /// Note that usernames are NOT length restricted by default in Hive.
        /// If you wish to restrict a username's length, <see cref="IUserPlugin.AllowUsername(string)"/>
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// An alternative ID of the user.
        /// In Auth0's case, this would be the auth0 unique ID, which would then be mappable to this particular username/user structure.
        /// Note that a given user's <see cref="AlternativeId"/> cannot be changed once the user has been created and tracked.
        /// </summary>
        [Key]
        public string AlternativeId { get; set; } = null!;

        /// <summary>
        /// The additional data attached to the user object.
        /// This is a dictionary as it holds each of the exact identities from Auth0 or any other authentication platform.
        /// Each of these can map to an arbitrary JSON object, so we map it like such.
        /// </summary>
        [Column(TypeName = "jsonb")]
        [JsonConverter(typeof(ArbitraryAdditionalData.ArbitraryAdditionalDataConverter))]
        public ArbitraryAdditionalData AdditionalData { get; set; } = new();

        /// <summary>
        /// A list of all the mods that the user has uploaded.
        /// </summary>
        public IList<Mod> Uploaded { get; set; } = new List<Mod>();
        /// <summary>
        /// A list of all the mods that the user has authored.
        /// </summary>
        public IList<Mod> Authored { get; set; } = new List<Mod>();
        /// <summary>
        /// A list of all the mods that the user has contriubted to.
        /// </summary>
        public IList<Mod> ContributedTo { get; set; } = new List<Mod>();

        /// <summary>
        /// Configures for EF
        /// </summary>
        /// <param name="b"></param>
        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            _ = b.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            _ = b.Entity<User>()
                .HasIndex(u => u.AlternativeId);
        }
    }
}
