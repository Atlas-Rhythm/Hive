using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // TODO: Consider adding validations here?

        public Uri Domain { get; private set; }
        public string Audience { get; private set; }
        public string ClientID { get; private set; }
        public string ClientSecret { get; private set; }
        public int? TimeoutMS { get; private set; }
        public Uri BaseDomain { get; private set; }
    }

    /// <summary>
    /// Extension type for adding Auth0 configuration.
    /// </summary>
    public static class Auth0OptionsExtensions
    {
        /// <summary>
        /// Helper method for installing <see cref="Auth0Options"/> configuration.
        /// </summary>
        /// <param name="services"></param>
        public static OptionsBuilder<Auth0Options> AddAuth0Config(this IServiceCollection services)
        {
            return services.AddOptions<Auth0Options>()
                .BindConfiguration(Auth0Options.ConfigHeader, a => a.BindNonPublicProperties = true)
                .ValidateDataAnnotations();
        }
    }
}
