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

        [Url]
        [Required]
        public Uri? Domain { get; private set; }

        [Required(AllowEmptyStrings = false)]
        public string? Audience { get; private set; }

        [Required(AllowEmptyStrings = false)]
        public string? ClientID { get; private set; }

        [Required(AllowEmptyStrings = false)]
        public string? ClientSecret { get; private set; }

        [Range(0, int.MaxValue, ErrorMessage = "TimeoutMS must be zero or positive!")]
        public int TimeoutMS { get; private set; } = 10000;

        [Required]
        public Uri? BaseDomain { get; private set; }
    }
}
