using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hive.Extensions;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Hive
{
    /// <summary>
    /// Middleware for restricting access to guest (non-authenticated) users.
    /// </summary>
    public class GuestRestrictionMiddleware
    {
        private const string routeSeparator = "/";
        private const string wildcardToken = "*";
        private const char cascadingSuffix = '/';
        private const char explicitUnrestrictedPrefix = '!';
        private const char queryParameterToken = '?';

        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private readonly RequestDelegate next;
        private readonly Serilog.ILogger logger;
        private readonly IProxyAuthenticationService auth;
        private readonly Node rootRestrictionNode;

        /// <summary>
        /// Creates a Middleware instance using DI.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        /// <param name="auth"></param>
        /// <param name="configuration"></param>
        public GuestRestrictionMiddleware([DisallowNull] RequestDelegate next, [DisallowNull] Serilog.ILogger logger,
            [DisallowNull] IProxyAuthenticationService auth, [DisallowNull] IConfiguration configuration)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.next = next;
            this.logger = logger.ForContext<GuestRestrictionMiddleware>();
            this.auth = auth;

            rootRestrictionNode = new Node();

            // This configuration option is simply a list of routes ("/api/mod", "/api/upload", etc.)
            var restrictedRoutes = configuration.GetSection("RestrictedRoutes").Get<List<string>>();

            foreach (var route in restrictedRoutes)
            {
                DecomposeRouteIntoNodeTree(route);
            }
        }

        /// <summary>
        /// Invokes the delegate, only if
        ///     (1) the route is not marked as restricted in the config, or
        ///     (2) if there is an authenticated user behind the request.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (!httpContext.Response.HasStarted)
            {
                // Grab the route the user is wanting to access
                var route = httpContext.Request.Path.Value!
                    // Remove case insensitivity
                    .ToUpperInvariant()
                    // Ignore all query parameters
                    .Split(queryParameterToken).First();

                // Split our route into the individual components
                var routeView = new StringView(route);
                var routeComponents = routeView.Split(routeSeparator);

                var currentNode = rootRestrictionNode;
                Node? cascadingNode = null;

                // Let's iterate down the chain and find which node we should use.
                foreach (var component in routeComponents)
                {
                    // Keep going down the chain if we can safely do so.
                    if (currentNode.Children.ContainsKey(component))
                    {
                        // If this node happens to cascade, we save it for later.
                        if (currentNode.CascadesToChildren)
                        {
                            cascadingNode = currentNode;
                        }

                        currentNode = currentNode.Children[component];
                        continue;
                    }

                    // If we encounter no children, but have a wildcard, we continue down the wildcard path.
                    if (currentNode.Wildcard != null)
                    {
                        // If the wildcard happens to cascade, we save it for later.
                        if (currentNode.Wildcard.CascadesToChildren)
                        {
                            cascadingNode = currentNode.Wildcard;
                        }

                        currentNode = currentNode.Wildcard;
                        continue;
                    }

                    // If we hit a dead end, we see if we should use the last parent node that cascades down.
                    // By caching our last cascading node, we don't have to re-iterate back up the chain.
                    if (cascadingNode != null)
                    {
                        currentNode = cascadingNode;
                    }

                    // At this point, we've hit a dead end, so we should break
                    break;
                }

                // If we have a valid node, we see whether or not it's restricted to authenticated users.
                if (currentNode != null && currentNode.Restricted)
                {
                    // Grab our Hive user from the request (Either cached, or forwarded to the auth service)
                    var user = await httpContext.Request.GetHiveUser(auth).ConfigureAwait(false);

                    // If the user is not authenticated, and trying to access a restricted endpoint, return 401 Unauthorized.
                    if (user == null)
                    {
                        logger.Error("Non-authenticated user prevented access to {0}", route);

                        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        httpContext.Response.ContentType = "application/json";

                        var jsonException = new JsonApiException
                        {
                            StatusCode = httpContext.Response.StatusCode,
                            Message = Resources.SR.RestrictionMiddleware_Unauthorized
                        };

                        await httpContext.Response.WriteAsJsonAsync(jsonException, serializerOptions).ConfigureAwait(false);

                        return;
                    }
                }
            }

            await next.Invoke(httpContext).ConfigureAwait(false);
        }

        // Take a route and decompose it into separate parts for our node tree.
        private void DecomposeRouteIntoNodeTree(string route)
        {
            if (string.IsNullOrWhiteSpace(route)) return;

            // We need to process our route a little bit before we decompose it into the node tree.
            var processedRoute = route
                // Remove case sensitivity
                .ToUpperInvariant()
                // Ignore all query parameters
                .Split(queryParameterToken).First();

            var routeView = new StringView(processedRoute);

            var isRestricted = true;
            var cascades = false;

            if (routeView[0] == explicitUnrestrictedPrefix)
            {
                // This endpoint (and potentially all subroutes) are explicitly unrestricted.
                isRestricted = false;
                routeView = routeView[1..];
            }

            if (routeView.Last() == cascadingSuffix)
            {
                // All children to this route will inherit this route's Restricted status.
                cascades = true;
                routeView = routeView[0..^1];
            }

            // Split our route into the individual components
            var routeComponents = routeView.Split(routeSeparator, true);
            var currentNode = rootRestrictionNode;
            var count = routeComponents.Count();

            // Keep our own iteration variable, as we may need to check for ambiguity at our last iteration
            var i = 0;

            foreach (var component in routeComponents)
            {
                // Continue down our node tree if we can safely do so.
                if (currentNode.Children.ContainsKey(component))
                {
                    currentNode = currentNode.Children[component];

                    // We only need to do an ambiguity check if we are on our last component and it already exists
                    if (i == count - 1 && currentNode.Parent != null)
                    {
                        TestForAmbiguity(routeView, currentNode.Parent, currentNode, isRestricted);
                    }

                    i++;

                    continue;
                }

                // Ensure that our path down to the last node is created.
                // Default behavior is that these Nodes are not restricted, and do not cascade.
                var node = new Node
                {
                    Parent = currentNode
                };

                // Wildcard is a special case, should assign wildcard (if its null) then continue
                if (component == wildcardToken)
                {
                    // We may need to do an ambiguity check with our wildcard node
                    if (currentNode.Wildcard != null && i == count - 1)
                    {
                        TestForAmbiguity(routeView, currentNode, currentNode.Wildcard, isRestricted);
                    }

                    currentNode.Wildcard ??= node;
                }
                else
                {
                    currentNode.Children.Add(component, node);
                }

                currentNode = node;

                i++;
            }

            // Once we've reached the last node, and it passes the ambiguity check, we can now assign values.
            currentNode.Restricted = isRestricted;
            currentNode.CascadesToChildren = cascades;
        }

        private static void TestForAmbiguity(StringView routeView, Node parentNode, Node currentNode, bool isRestricted)
        {
            // We might be at risk of ambiguity if:
            //   1) We have hit the last component to the route we are parsing
            //   2) Our current node is either a child of the parent node OR the parent's wildcard node
            if (currentNode.Parent == parentNode || parentNode.Wildcard == currentNode)
            {
                // If we have conflicting restriction values, we throw.
                // I don't wanna deal with ambiguity lmao
                if (currentNode.Restricted != isRestricted)
                {
                    throw new InvalidOperationException($"Ambiguity exists at endpoint {routeView}.");
                }
            }
        }

        private class Node
        {
            // Whether or not this particular endpoint is restricted to authenticated users.
            public bool Restricted;

            // If true, the Restricted will be inherited to subroutes, unless otherwise specified.
            public bool CascadesToChildren;

            // A wildcard Node, assigned to *, which (if defined) handles all routes at that level.
            public Node? Wildcard;

            // The parent Node. This Parent node should have this Node instance in the Children collection.
            public Node? Parent;

            // A dictionary of child nodes.
            public Dictionary<StringView, Node> Children = new();
        }
    }

    /// <summary>
    /// 
    /// </summary>    
    public static class GuestRestrictingMiddlewareExtensions
    {
        /// <summary>
        /// Extension method used to add <see cref="GuestRestrictionMiddleware"/> to the HTTP request pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGuestRestrictionMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<GuestRestrictionMiddleware>();
    }
}
