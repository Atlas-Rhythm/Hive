using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Hive
{
    internal class EmptyStatusCodeResponse : ActionResult, IStatusCodeActionResult
    {
        public int? StatusCode { get; init; }

        public EmptyStatusCodeResponse(int statusCode) => StatusCode = statusCode;
    }
}
