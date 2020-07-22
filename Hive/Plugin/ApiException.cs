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
            switch (code)
            {
                case HttpStatusCode.NotFound:
                    return "Not found";

                case HttpStatusCode.BadRequest:
                    return "Bad request";

                case HttpStatusCode.Forbidden:
                    return "Forbidden";

                case HttpStatusCode.InternalServerError:
                    return "Internal server error";

                default:
                    return null;
            }
        }
    }
}