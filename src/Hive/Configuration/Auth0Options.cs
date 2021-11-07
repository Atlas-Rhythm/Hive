using System;
using System.ComponentModel.DataAnnotations;

namespace Hive.Configuration
{
    /// <summary>
    /// Represents configuration for Auth0.
    /// </summary>
    public class Auth0Options
    {
        /// <summary>
        /// The config header to look under for this configuration instance.
        /// </summary>
        public const string ConfigHeader = "Auth0";

        /// <summary>
        /// The Auth0 Domain.
        /// </summary>
        [Required]
        public Uri? Domain { get; private set; }

        /// <summary>
        /// The Auth0 Audience.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string? Audience { get; private set; }

        /// <summary>
        /// The Auth0 Client ID.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string? ClientID { get; private set; }

        /// <summary>
        /// The Auth0 Client Secret.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string? ClientSecret { get; private set; }

        /// <summary>
        /// The Auth0 Timeout in MS.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "TimeoutMS must be zero or positive!")]
        public int TimeoutMS { get; private set; } = 10000;

        /// <summary>
        /// The domain to redirect to after callbacks. Should be the publicly facing uri of this Hive instance.
        /// </summary>
        [Required]
        public Uri? BaseDomain { get; private set; }
    }
}
