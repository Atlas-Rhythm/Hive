using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hive.Plugin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hive
{
    internal struct JsonApiException
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionHandlingMiddleware
    {
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, Serilog.ILogger log)
        {
            _next = next;
            logger = (log ?? throw new ArgumentException(Resource.ArgumentNullException_logger, nameof(log))).ForContext<ExceptionHandlingMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            string? message = null;
            try
            {
                await _next.Invoke(httpContext);
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

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}