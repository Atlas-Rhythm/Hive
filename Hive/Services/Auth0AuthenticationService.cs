using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;

namespace Hive.Services
{
    /// <summary>
    /// An authentication service for linking with Auth0.
    /// </summary>
    public sealed class Auth0AuthenticationService : IProxyAuthenticationService, IDisposable
    {
        private const string authenticationAPIUserEndpoint = "userinfo";
        private const string managementAPIGetManagementToken = "oauth/token";
        private const string managementAPIUserEndpoint = "api/v2/users";

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly Uri domain;
        private readonly HttpClient client;
        private readonly string audience;
        private readonly string clientID;
        private readonly string clientSecret;
        private readonly ILogger logger;
        private readonly IClock clock;

        private string? managementToken;
        private Instant? managementExpireInstant;

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

            domain = section.GetValue<Uri>("Domain");
            audience = section.GetValue<string>("Audience");
            // Hive needs to use a Machine-to-Machine Application to grab a Management API v2 token
            // in order to retrieve users by their IDs.
            clientID = section.GetValue<string>("ClientID");
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
                throw new ArgumentNullException(nameof(request));

            if (managementToken is null || clock.GetCurrentInstant() > managementExpireInstant)
                await RefreshManagementAPIToken().ConfigureAwait(false);

            using var message = new HttpRequestMessage(HttpMethod.Get, authenticationAPIUserEndpoint);

            if (request.Headers.TryGetValue("Authorization", out var auth))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);

            try
            {
                var response = await client.SendAsync(message).ConfigureAwait(false);

                var auth0User = await response.Content.ReadFromJsonAsync<Auth0User>(jsonSerializerOptions).ConfigureAwait(false);

                // REVIEW: is this dumb
                return auth0User is null
                    ? null
                    : new User
                    {
                        Username = auth0User.Username,
                        AdditionalData = auth0User.User_Metadata,
                        AuthenticationType = "Bearer",
                        IsAuthenticated = true
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
            if (clock.GetCurrentInstant() >= managementExpireInstant)
            {
                await RefreshManagementAPIToken().ConfigureAwait(false);
            }

            // TODO: Test this query string, possible other fields to search: "nickname" and "name"
            // We don't need the whole kitchen sink here, so let's reduce fields to what we need
            var query = $"q=username:\"{userId}\"&search_engine=v3&include_fields=true&fields=username,user_metadata";

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
                return auth0User is null
                    ? null
                    : new User
                    {
                        Username = auth0User.Username,
                        AdditionalData = auth0User.User_Metadata,
                        IsAuthenticated = false
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

        // Helper method to refresh Hive's management API token.
        // It's main purpose is for getting a user by their ID, since that endpoint requires this special kind of token.
        // This token expires every 24 hours, so each day at least 1 request might be a little bit slower.
        // For more info, see https://auth0.com/docs/tokens/management-api-access-tokens
        private async Task RefreshManagementAPIToken()
        {
            logger.Information("Refreshing Auth0 Management API Token...");

            using var message = new HttpRequestMessage(HttpMethod.Post, managementAPIGetManagementToken);
            message.Headers.Add("content-type", "application/x-www-form-urlencoded");

            var data = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "audience", audience },
            };

            message.Content = new FormUrlEncodedContent(data!);

            // REVIEW: should i include proper error handling or just let it bubble to the method that uses this
            var response = await client.SendAsync(message).ConfigureAwait(false);

            var body = await response.Content.ReadFromJsonAsync<ManagementAPIResponse>(jsonSerializerOptions).ConfigureAwait(false);

            managementToken = body!.Access_Token;
            managementExpireInstant = clock.GetCurrentInstant() + Duration.FromSeconds(body!.Expires_In);
        }

        // REVIEW: Should these be moved to Hive.Models?
        private record ManagementAPIResponse(string Access_Token, int Expires_In, string Scope, string Token_Type);

        private record Auth0User(string Username, JsonElement User_Metadata);
    }
}
