using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hive
{
    internal struct JsonApiException
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Middleware for wrapping exceptions.
    /// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger logger;
        private readonly JsonSerializerOptions serializerOptions;

        /// <summary>
        /// Create using a given <see cref="RequestDelegate"/> and <see cref="Serilog.ILogger"/>
        /// </summary>
        /// <param name="next"></param>
        /// <param name="log"></param>
        /// <param name="serializerOptions"></param>
        public ExceptionHandlingMiddleware([DisallowNull] RequestDelegate next, [DisallowNull] Serilog.ILogger log, JsonSerializerOptions serializerOptions)
        {
            if (next is null)
                throw new ArgumentNullException(nameof(next));
            if (log is null)
                throw new ArgumentNullException(nameof(log));
            _next = next;
            logger = log.ForContext<ExceptionHandlingMiddleware>();
            this.serializerOptions = serializerOptions;
        }

        /// <summary>
        /// Invokes the delegate, or handles an exception on failure.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to catch all exceptions and handle them as internal server errors.")]
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));
            string? message = null;
            try
            {
                await _next.Invoke(httpContext).ConfigureAwait(false);
            }
            catch (ApiException ex)
            {
                message = ex.Message;
                logger.Error(ex, "Handling API Exception");
                httpContext.Response.StatusCode = ex.StatusCode;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                logger.Error(ex, "Internal server exception");
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            // TODO: This should be changed for GraphQL endpoints probably
            if (!string.IsNullOrEmpty(message) && !httpContext.Response.HasStarted)
            {
                httpContext.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new JsonApiException { StatusCode = httpContext.Response.StatusCode, Message = message }, serializerOptions);
                await httpContext.Response.WriteAsync(json).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public static class ExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Extension method used to add the middleware to the HTTP request pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder) => builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
