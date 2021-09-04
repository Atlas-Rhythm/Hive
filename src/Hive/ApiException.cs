using System;
using System.Net;

namespace Hive
{
    /// <summary>
    /// An exception wrapper for common API exceptions.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// The status code of the response
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// A message to display
        /// </summary>
        public new string? Message { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        public ApiException(HttpStatusCode statusCode, string? message = null)
        {
            StatusCode = (int)statusCode;
            Message = message ?? StatusCodeDefaultMessage(statusCode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Fallback is null")]
        private static string? StatusCodeDefaultMessage(HttpStatusCode code)
        {
            return code switch
            {
                HttpStatusCode.NotFound => "Not found",
                HttpStatusCode.BadRequest => "Bad request",
                HttpStatusCode.Forbidden => "Forbidden",
                HttpStatusCode.InternalServerError => "Internal server error",
                _ => null,
            };
        }

        /// <inheritdoc/>
        public ApiException()
        {
        }

        /// <inheritdoc/>
        public ApiException(string message) : base(message)
        {
        }

        /// <inheritdoc/>
        public ApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
