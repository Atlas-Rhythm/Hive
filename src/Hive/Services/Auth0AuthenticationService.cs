using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Hive.Plugins.Aggregates;
using Hive.Extensions;

namespace Hive.Services
{
    /// <summary>
    /// Represents a plugin that handles user creation.
    /// </summary>
    [Aggregable]
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
        void ExtraDataModification(Dictionary<string, object?> extraData) { }
    }

    internal class HiveUsernamePlugin : IUserCreationPlugin { }

    /// <summary>
    /// An authentication service for linking with Auth0.
    /// </summary>
    public sealed class Auth0AuthenticationService : IProxyAuthenticationService, IAuth0Service, IDisposable
    {
        private const string authenticationAPIUserEndpoint = "userinfo";
        private const string authenticationAPIGetToken = "oauth/token";
        //private const string managementAPIUserEndpoint = "api/v2/users";

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient client;
        private readonly string clientSecret;
        private readonly ILogger logger;
        private readonly IClock clock;
        private readonly HiveContext context;
        private readonly IAggregate<IUserCreationPlugin> userCreationPlugin;

        private string? managementToken;
        private Instant? managementExpireInstant;

        /// <inheritdoc/>
        public Auth0ReturnData Data { get; }

        private readonly Dictionary<string, string> refreshTokenJsonBody;

        /// <summary>
        /// A semaphore that only allows for one job to perform work for refreshing the token.
        /// </summary>
        private static readonly SemaphoreSlim refreshTokenSemaphore = new(1);

        /// <summary>
        /// Construct a <see cref="Auth0AuthenticationService"/> with DI.
        /// </summary>
        public Auth0AuthenticationService(
            [DisallowNull] ILogger log,
            IClock clock,
            IConfiguration configuration,
            HiveContext context,
            IAggregate<IUserCreationPlugin> userCreationPlugin)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));

            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            this.clock = clock;
            this.context = context;
            logger = log.ForContext<Auth0AuthenticationService>();

            var section = configuration.GetSection("Auth0");

            var domain = section.GetValue<Uri>("Domain");
            var audience = section.GetValue<string>("Audience");
            // Hive needs to use a Machine-to-Machine Application to grab a Management API v2 token
            // in order to retrieve users by their IDs.
            var clientID = section.GetValue<string>("ClientID");
            Data = new Auth0ReturnData(domain.ToString(), clientID, audience);

            clientSecret = section.GetValue<string>("ClientSecret");

            // Create refresh token json body, used for sending requests of the proper type/shape
            refreshTokenJsonBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", Data.ClientId },
                { "client_secret", clientSecret },
                { "audience", Data.Audience }
            };

            var timeout = new TimeSpan(0, 0, 0, 0, section.GetValue("TimeoutMS", 10000));
            client = new HttpClient
            {
                BaseAddress = domain,
                DefaultRequestVersion = new Version(2, 0),
                Timeout = timeout,
            };
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
                // Note that this call CAN throw exceptions.
                // If it does, we should return null immediately, since we need a valid management API token.
                await EnsureValidManagementAPIToken().ConfigureAwait(false);

                using var message = new HttpRequestMessage(HttpMethod.Get, authenticationAPIUserEndpoint);

                if (request.Headers.TryGetValue("Authorization", out var auth))
                    message.Headers.Add("Authorization", new List<string> { auth });

                var response = await client.SendAsync(message).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                // We should obtain both the nickname and the sub
                var auth0User = await response.Content.ReadFromJsonAsync<Auth0User>(jsonSerializerOptions).ConfigureAwait(false);

                if (auth0User is null)
                {
                    // We can either throw here because we MUST have a valid auth0 user, or return null
                    throw new InvalidOperationException("Auth0 user not found!");
                }
                // We should perform a DB lookup on the sub to see if we can find a User object with that sub.
                // Note that the found object is WITH tracking, so modifications can be applied and saved.
                // TODO: This may not be what we want.
                // This will throw if we have more than one matching sub
                var matching = await context.Users.Where(u => u.AlternativeId == auth0User.Sub).SingleOrDefaultAsync().ConfigureAwait(false);
                if (matching is not null)
                {
                    return matching;
                }
                else
                {
                    // If we cannot find an existing sub, we make a new username and ensure no duplicates.
                    // Also note that accounts need to be LINKED in order for them to be considered the same (ex, Discord and GH accounts linked on frontend)
                    // Once accounts are linked, they have the same sub
                    var u = new User
                    {
                        Username = userCreationPlugin.Instance.ChooseUsername(auth0User.Nickname),
                        AlternativeId = auth0User.Sub,
                        AdditionalData = auth0User.User_Metadata.ToDictionary(p => p.Key, p => (object?)p.Value)
                    };

                    while (await context.Users.AsNoTracking().ContainsAsync(u).ConfigureAwait(false))
                    {
                        u.Username += Guid.NewGuid();
                    }

                    userCreationPlugin.Instance.ExtraDataModification(u.AdditionalData);

                    _ = await context.Users.AddAsync(u).ConfigureAwait(false);
                    _ = await context.SaveChangesAsync().ConfigureAwait(false);
                    return u;
                }
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
                return await context.Users.Where(u => u.Username == userId).SingleOrDefaultAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private async Task EnsureValidManagementAPIToken()
        {
            try
            {
                if (managementToken == null || clock.GetCurrentInstant() >= managementExpireInstant)
                {
                    await RefreshManagementAPIToken().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "An exception occured while attempting to refresh our Auth0 Management API Token.");
                throw;
            }
        }

        // Helper method to refresh Hive's management API token.
        // It's main purpose is for getting a user by their ID, since that endpoint requires this special kind of token.
        // This token expires every 24 hours, so each day at least 1 request might be a little bit slower.
        // For more info, see https://auth0.com/docs/tokens/management-api-access-tokens
        private async Task RefreshManagementAPIToken()
        {
            // Exit if we are using the semaphore already.
            if (refreshTokenSemaphore.CurrentCount == 0)
                return;
            logger.Information("Refreshing Auth0 Management API Token...");
            // Only if we are not currently getting a management API token do we call this, thus the TryEnter as opposed to a lock.
            // If we do NOT enter, that means that another thread wants to refresh the token while this is currently being used.
            await refreshTokenSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, authenticationAPIGetToken)
                {
                    Content = JsonContent.Create(refreshTokenJsonBody)
                };

                // Any exception here will bubble to caller.
                var response = await client.SendAsync(message).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    logger.Error("Failed to retrieve new auth0 token! Failed status code: {StatusCode}", response.StatusCode);
                    // Short circuit exit without fixing management token on failure to retreive one later.
                    // TODO: This is ultimately something we may want to consider making a new exception type for.
                    throw new InvalidOperationException(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }

                var body = await response.Content.ReadFromJsonAsync<ManagementAPIResponse>(jsonSerializerOptions).ConfigureAwait(false);

                managementToken = body!.Access_Token;
                managementExpireInstant = clock.GetCurrentInstant() + Duration.FromSeconds(body!.Expires_In);
            }
            finally
            {
                _ = refreshTokenSemaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Auth0TokenResponse?> RequestToken(Uri sourceUri, string code, string? state)
        {
            if (sourceUri is null)
                throw new ArgumentNullException(nameof(sourceUri));
            logger.Debug("Requesting auth token for user...");
            var data = new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "client_id", Data.ClientId },
                { "client_secret", clientSecret },
                { "redirect_uri", sourceUri.GetComponents(UriComponents.Scheme | UriComponents.HostAndPort | UriComponents.Path, UriFormat.UriEscaped) }
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, authenticationAPIGetToken)
            {
                Content = JsonContent.Create(data)
            };
            var response = await client.SendAsync(message).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Failed to retrieve client auth0 token! Failed status code: {StatusCode}", response.StatusCode);
                // Short circuit exit without fixing management token on failure to retreive one later.
                return null;
            }
            return await response.Content.ReadFromJsonAsync<Auth0TokenResponse>(jsonSerializerOptions).ConfigureAwait(false);
        }

        private record ManagementAPIResponse(string Access_Token, int Expires_In, string Scope, string Token_Type);

        private record Auth0User
        {
            public string Nickname { get; set; }

            public string Sub { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> User_Metadata { get; set; } = new Dictionary<string, JsonElement>();

            public Auth0User(string nickname, string sub)
            {
                Nickname = nickname;
                Sub = sub;
            }
        }
    }
}
