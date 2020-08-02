using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Hive.Plugin
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public new string? Message { get; }

        public ApiException(HttpStatusCode statusCode, string? message = null)
        {
            StatusCode = (int)statusCode;
            Message = message ?? StatusCodeDefaultMessage(statusCode);
        }

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
    }
}