using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hive.Services
{
    public sealed class VaulthAuthenticationService : IProxyAuthenticationService, IDisposable
    {
        private const string vaulthGetUserApi = "user";

        // TODO: Bother raft for this correct value + query for username
        private const string vaulthGetGenericUserApi = "user";

        private readonly Uri vaulthUri;
        private readonly HttpClient client;
        private readonly ILogger logger;

        public VaulthAuthenticationService([DisallowNull] ILogger log, IConfiguration configuration)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));
            logger = log.ForContext<VaulthAuthenticationService>();
            vaulthUri = configuration.GetValue<Uri>("vaulthBaseUri");
            var timeout = new TimeSpan(0, 0, 0, 0, configuration.GetValue<int>("vaulthTimeoutMs"));
            client = new HttpClient
            {
                BaseAddress = vaulthUri,
                Timeout = timeout,
                // TODO: Configure request version? As of now, vaulth will always support HTTP/2.0
                DefaultRequestVersion = new Version(2, 0)
            };
        }

        public Task<bool> IsValid(HttpRequest request)
        {
            // Check the header of the request for a token
            // If the token exists, verify it with a check
            // If the token matches, we should be good to say that the token is valid and that the user is logged in
            // TODO: Implement
            return Task.FromResult(false);
        }

        public async Task<User?> GetUser(string userId, bool throwOnError = false)
        {
            if (string.IsNullOrEmpty(userId))
                if (throwOnError)
                    throw new ArgumentNullException(nameof(userId));
                else
                    return null;
            try
            {
                // TODO: Add username as query parameter, or body, dependening on what vaulth decides.
                return await client.GetFromJsonAsync<User?>(vaulthGetGenericUserApi, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                }).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                logger.Error(e, "HTTP Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
            catch (JsonException e)
            {
                logger.Error(e, "JSON Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
            catch (TaskCanceledException e)
            {
                logger.Error(e, "Task Cancelled Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
        }

        public async Task<User?> GetUser(HttpRequest request, bool throwOnError = false)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            // After we are certain we have a valid token in our request header, we can get the user using it
            // If the response we get is valid, we are happy to continue, otherwise, we have a failure and return null/task failure
            // Invoke user/get with the current request to get info about the user (the entire structure)
            using var message = new HttpRequestMessage(HttpMethod.Get, vaulthGetUserApi);
            // Authorization header
            if (request.Headers.TryGetValue("Authorization", out var auth))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            try
            {
                var resp = await client.SendAsync(message).ConfigureAwait(false);
                return await resp.Content.ReadFromJsonAsync<User?>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                }).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                logger.Error(e, "HTTP Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
            catch (JsonException e)
            {
                logger.Error(e, "JSON Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
            catch (TaskCanceledException e)
            {
                logger.Error(e, "Task Cancelled Exception!");
                if (throwOnError)
                    throw;
                return null;
            }
        }

        public void Dispose() => client.Dispose();
    }
}