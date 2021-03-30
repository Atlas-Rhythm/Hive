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

namespace Hive.Services
{
    /// <summary>
    /// An authentication service for linking with Auth0.
    /// </summary>
    public sealed class Auth0AuthenticationService : IProxyAuthenticationService, IAuth0Service, IDisposable
    {
        private const string authenticationAPIUserEndpoint = "userinfo";
        private const string authenticationAPIGetToken = "oauth/token";
        private const string managementAPIUserEndpoint = "api/v2/users";

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient client;
        private readonly string clientSecret;
        private readonly ILogger logger;
        private readonly IClock clock;

        private string? managementToken;
        private Instant? managementExpireInstant;

        /// <inheritdoc/>
        public bool Enabled => true;

        /// <inheritdoc/>
        public Auth0ReturnData Data { get; }

        /// <summary>
        /// Construct a <see cref="Auth0AuthenticationService"/> with DI.
        /// </summary>
        public Auth0AuthenticationService([DisallowNull] ILogger log, IClock clock, IConfiguration configuration)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));

            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            this.clock = clock;
            logger = log.ForContext<Auth0AuthenticationService>();

            var section = configuration.GetSection("Auth0");

            var domain = section.GetValue<Uri>("Domain");
            var audience = section.GetValue<string>("Audience");
            // Hive needs to use a Machine-to-Machine Application to grab a Management API v2 token
            // in order to retrieve users by their IDs.
            var clientID = section.GetValue<string>("ClientID");
            Data = new Auth0ReturnData(domain.ToString(), clientID, audience);
            clientSecret = section.GetValue<string>("ClientSecret");

            var timeout = new TimeSpan(0, 0, 0, 0, section.GetValue("TimeoutMS", 10000));
            client = new HttpClient
            {
                BaseAddress = domain,
                DefaultRequestVersion = new Version(2, 0),
                Timeout = timeout,
            };
        }

        /// <inheritdoc/>
        public void Dispose() => client.Dispose();

        /// <inheritdoc/>
        public async Task<User?> GetUser(HttpRequest request, bool throwOnError = false)
        {
            if (request is null)
                return throwOnError ? throw new ArgumentNullException(nameof(request)) : null;

            await EnsureValidManagementAPIToken(throwOnError).ConfigureAwait(false);

            using var message = new HttpRequestMessage(HttpMethod.Get, authenticationAPIUserEndpoint);

            if (request.Headers.TryGetValue("Authorization", out var auth))
                message.Headers.Add("Authorization", new List<string> { auth });

            try
            {
                var response = await client.SendAsync(message).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var auth0User = await response.Content.ReadFromJsonAsync<Auth0User>(jsonSerializerOptions).ConfigureAwait(false);

                // REVIEW: is this dumb
                return auth0User is null || string.IsNullOrEmpty(auth0User.Nickname)
                    ? null
                    : new User
                    {
                        Username = auth0User.Nickname,
                        AdditionalData = auth0User.User_Metadata
                    };
            }
            catch (Exception e)
            {
                logger.Error(e, "An exception occured while attempting to retrieve user information.");
                if (throwOnError)
                    throw;
                return null;
            }
        }

        /// <inheritdoc/>
        // TODO: document: THE MACHINE-TO-MACHINE APPLICATION NEEDS THE "read:users" SCOPE
        public async Task<User?> GetUser(string userId, bool throwOnError = false)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return throwOnError ? throw new ArgumentNullException(nameof(userId)) : null;
            }

            // Refresh our management API token if it has expired.
            await EnsureValidManagementAPIToken(throwOnError).ConfigureAwait(false);

            // TODO: Test this query string, possible other fields to search: "nickname" and "name"
            // We don't need the whole kitchen sink here, so let's reduce fields to what we need
            var query = $"q=username:\"{userId}\"&search_engine=v3&include_fields=true&fields=nickname,user_metadata";

            using var message = new HttpRequestMessage(HttpMethod.Get,
                Uri.EscapeDataString($"{managementAPIUserEndpoint}?{query}"));

            try
            {
                var response = await client.SendAsync(message).ConfigureAwait(false);

                // The endpoint returns a collection of users that match our query.
                var auth0Users = await response.Content.ReadFromJsonAsync<Auth0User[]>(jsonSerializerOptions).ConfigureAwait(false);

                // REVIEW: should I panic if there's multiple returned users (they all have to share the same EXACT username)
                var auth0User = auth0Users?.FirstOrDefault();

                // REVIEW: is this dumb
                return auth0User is null || string.IsNullOrEmpty(auth0User.Nickname)
                    ? null
                    : new User
                    {
                        Username = auth0User.Nickname,
                        AdditionalData = auth0User.User_Metadata,
                    };
            }
            catch (Exception e)
            {
                logger.Error(e, "An exception occured while attempting to retrieve user information.");
                if (throwOnError)
                    throw;
                return null;
            }
        }

        private async Task EnsureValidManagementAPIToken(bool throwOnError = true)
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
                if (throwOnError) throw;
            }
        }

        // Helper method to refresh Hive's management API token.
        // It's main purpose is for getting a user by their ID, since that endpoint requires this special kind of token.
        // This token expires every 24 hours, so each day at least 1 request might be a little bit slower.
        // For more info, see https://auth0.com/docs/tokens/management-api-access-tokens
        private async Task RefreshManagementAPIToken()
        {
            logger.Information("Refreshing Auth0 Management API Token...");

            using var message = new HttpRequestMessage(HttpMethod.Post, authenticationAPIGetToken);

            var data = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", Data.ClientId },
                { "client_secret", clientSecret },
                { "audience", Data.Audience },
            };

            message.Content = JsonContent.Create(data);

            // Any exception here will bubble to caller.
            var response = await client.SendAsync(message).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Failed to retrieve new auth0 token! Failed status code: {StatusCode}", response.StatusCode);
                // Short circuit exit without fixing management token on failure to retreive one later.
                return;
            }

            var body = await response.Content.ReadFromJsonAsync<ManagementAPIResponse>(jsonSerializerOptions).ConfigureAwait(false);

            managementToken = body!.Access_Token;
            managementExpireInstant = clock.GetCurrentInstant() + Duration.FromSeconds(body!.Expires_In);
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

        // REVIEW: Should these be moved to Hive.Models?
        private record ManagementAPIResponse(string Access_Token, int Expires_In, string Scope, string Token_Type);

        private record Auth0User
        {
            public string Nickname { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> User_Metadata { get; set; } = new Dictionary<string, JsonElement>();

            public Auth0User(string nickname)
            {
                Nickname = nickname;
            }
        }
    }
}
