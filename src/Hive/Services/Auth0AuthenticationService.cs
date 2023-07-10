using Hive.Models;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Hive.Plugins.Aggregates;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Hive.Configuration;
using LitJWT;

namespace Hive.Services
{
    /// <summary>
    /// Represents a plugin that handles user creation.
    /// </summary>
    [Aggregable(Default = typeof(HiveUsernamePlugin))]
    public interface IUserCreationPlugin
    {
        /// <summary>
        /// This function is called once when a new user is to be created. This function should return the username conversion from the original username, if there should be one.
        /// Hive will try to create a new user with the returned username. However, if another user already exists, Hive will append a GUID after the username returned by this method.
        /// This is because Hive requires unique users. If you wish to control unique usernames yourself, return only unique usernames from this method.
        /// Hive default is to return the original username.
        /// </summary>
        /// <param name="originalUsername">The original username to convert, if necessary</param>
        /// <returns></returns>
        string ChooseUsername([TakesReturnValue] string originalUsername) => originalUsername;

        /// <summary>
        /// This function is called once when a new user is to be created. This function allows for editing of additional data before the instance is created.
        /// Hive default is to modify nothing.
        /// </summary>
        /// <param name="extraData">The original additional data to add to/remove from, if necessary</param>
        void ExtraDataModification(ArbitraryAdditionalData extraData) { }
    }

    internal class HiveUsernamePlugin : IUserCreationPlugin { }

    /// <summary>
    /// An authentication service for linking with Auth0.
    /// </summary>
    public sealed class Auth0AuthenticationService : IProxyAuthenticationService, IAuth0Service, IDisposable
    {
        private const string AuthenticationAPIGetToken = "oauth/token";

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient client;
        private readonly string clientSecret;
        private readonly ILogger logger;
        private readonly HiveContext context;
        private readonly JwtDecoder jwtDecoder;
        private readonly IAggregate<IUserCreationPlugin> userCreationPlugin;

        /// <inheritdoc/>
        public Auth0ReturnData Data { get; }

        /// <summary>
        /// Construct a <see cref="Auth0AuthenticationService"/> with DI.
        /// </summary>
        public Auth0AuthenticationService(
            ILogger log,
            IOptions<Auth0Options> config,
            HiveContext context,
            JwtDecoder jwtDecoder,
            IAggregate<IUserCreationPlugin> userCreationPlugin)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));

            if (config is null)
                throw new ArgumentNullException(nameof(config));

            this.context = context;
            this.jwtDecoder = jwtDecoder;
            logger = log.ForContext<Auth0AuthenticationService>();

            var options = config.TryLoad(logger, Auth0Options.ConfigHeader);
            // Hive needs to use a Machine-to-Machine Application to grab a Management API v2 token
            // in order to retrieve users by their IDs.
            Data = new Auth0ReturnData(options.Domain!.ToString(), options.ClientID!, options.Audience!);

            clientSecret = options.ClientSecret!;

            if (options.TimeoutMS > 0)
            {
                var timeout = new TimeSpan(0, 0, 0, 0, options.TimeoutMS);
                client = new HttpClient
                {
                    BaseAddress = options.Domain,
                    DefaultRequestVersion = new Version(2, 0),
                    Timeout = timeout,
                };
            }
            else
            {
                client = new HttpClient
                {
                    BaseAddress = options.Domain,
                    DefaultRequestVersion = new Version(2, 0),
                };
            }
            this.userCreationPlugin = userCreationPlugin;
        }

        /// <inheritdoc/>
        public void Dispose() => client.Dispose();

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We return null from this on ANY exception type instead of forwarding it to our callers.")]
        public async Task<User?> GetUser(HttpRequest? request)
        {
            if (request is null)
                // If we have a null request, we return a null user. This is the same as an unauthorized request.
                return null;
            try
            {
                var principal = request.HttpContext.User;
                var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                // We should perform a DB lookup on the sub to see if we can find a User object with that sub.
                // Note that the found object is WITH tracking, so modifications can be applied and saved.
                // TODO: This may not be what we want.
                // This will throw if we have more than one matching sub
                var matching = await context.Users.AsTracking().SingleOrDefaultAsync(u => u.AlternativeId == sub).ConfigureAwait(false);
                return matching;
            }
            catch (Exception e)
            {
                logger.Error(e, "An exception occured while attempting to retrieve user information.");
                return null;
            }
        }

        /// <inheritdoc/>
        // TODO: document: THE MACHINE-TO-MACHINE APPLICATION NEEDS THE "read:users" SCOPE
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We return null from this on ANY exception type instead of forwarding it to our callers.")]
        public async Task<User?> GetUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            try
            {
                // Should only have one matching user. If it has more than that, this will throw. Otherwise, will return null.
                return await context.Users.AsTracking().Where(u => u.Username == userId).SingleOrDefaultAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<Auth0TokenResponse?> RequestToken(string code, Uri redirectUri)
        {
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));

            logger.Debug("Requesting auth token for user... from: {RedirectUri}", redirectUri);

            var response = await client.PostAsync(AuthenticationAPIGetToken, new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri.ToString()),
                new KeyValuePair<string, string>("client_id", Data.ClientId),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            })).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Failed to retrieve client auth0 token! Failed status code: {StatusCode}", response.StatusCode);
                // Short circuit exit without fixing management token on failure to retreive one later.
                return null;
            }

            var auth0Tokens = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>(jsonSerializerOptions).ConfigureAwait(false);

            // Read the id token
            if (jwtDecoder.TryDecode<Auth0User>(auth0Tokens!.IdToken, out var payload) is not DecodeResult.Success)
                throw new InvalidOperationException("Could not validate id token");

            var sub = payload.Sub;
            var username = payload.Name;

            var matching = await context.Users.AsTracking().SingleOrDefaultAsync(u => u.AlternativeId == sub).ConfigureAwait(false);
            if (matching is null)
            {
                logger.Information("Creating new user with username {Username} and auth sub {Sub}", payload.Name, sub);

                // If we cannot find an existing sub, we make a new username and ensure no duplicates.
                // Also note that accounts need to be LINKED in order for them to be considered the same (ex, Discord and GH accounts linked on frontend)
                // Once accounts are linked, they have the same sub
                var u = new User
                {
                    AlternativeId = sub,
                    Username = userCreationPlugin.Instance.ChooseUsername(username),
                };
                u.AdditionalData.AddSerialized(payload.UserMetadata);

                // This must be done to avoid ambiguity with System.Linq.
                while (await context.Users.ContainsAsync(u).ConfigureAwait(false))
                {
                    u.Username += Guid.NewGuid();
                }

                userCreationPlugin.Instance.ExtraDataModification(u.AdditionalData);

                _ = await context.Users.AddAsync(u).ConfigureAwait(false);
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
                matching = u;
            }

            // Update profile picture if necessary
            var newPictureExists = payload.UserMetadata.TryGetValue("picture", out var newPictureJson);
            _ = matching.AdditionalData.TryGetValue<string>("picture", out var oldPicture);
            var newPicture = newPictureExists ? newPictureJson.GetString() : null;

            if (newPicture != oldPicture)
            {
                matching.AdditionalData.Set("picture", newPicture);
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return auth0Tokens;
        }

        private record Auth0User
        {
            [JsonPropertyName("sub")]
            public string Sub { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonExtensionData]
            [JsonPropertyName("User_Metadata")]
            public Dictionary<string, JsonElement> UserMetadata { get; set; } = new();

            public Auth0User(string name, string sub)
            {
                Sub = sub;
                Name = name;
            }
        }
    }
}
