using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Hive
{
    /// <summary>
    /// Represents a wrapper around a result with a message and status code. Used to signify successful or failed requests.
    /// </summary>
    /// <param name="Value">The returned value from a query.</param>
    /// <param name="Message">The message to return from a query.</param>
    /// <param name="StatusCode">The status code of the result from a query.</param>
    /// <typeparam name="T">The type of the value to wrap.</typeparam>
    public record HiveObjectQuery<T>(T? Value, string? Message, int StatusCode)
    {
        /// <summary>
        /// Was this query successful?
        /// </summary>
        public bool Successful => StatusCode is >= StatusCodes.Status200OK and <= 299;

        private ActionResult<TImpl> ConvertInternal<TImpl>(Func<T, TImpl> conversionFunc)
        {
            return StatusCode switch
            {
                StatusCodes.Status200OK => new OkObjectResult(conversionFunc.Invoke(Value!)),
                StatusCodes.Status204NoContent => new NoContentResult(),
                StatusCodes.Status400BadRequest => new BadRequestObjectResult(Message),
                StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(Message),
                StatusCodes.Status403Forbidden => new ForbidResult(),
                StatusCodes.Status404NotFound => new NotFoundObjectResult(Message),
                StatusCodes.Status424FailedDependency => new ObjectResult(conversionFunc.Invoke(Value!)) { StatusCode = StatusCode },
                _ => new ObjectResult(Message) { StatusCode = StatusCode },
            };
        }

        /// <summary>
        /// Converts this object to a <see cref="ActionResult{TValue}"/> of the same value type.
        /// </summary>
        /// <returns>The created <see cref="ActionResult{TValue}"/>.</returns>
        public ActionResult<T> Convert() => ConvertInternal(conv => conv);

        /// <summary>
        /// Converts this object to a <see cref="ActionResult{TValue}"/> of a differing type.
        /// </summary>
        /// <typeparam name="TCast">The new value type to convert to.</typeparam>
        /// <param name="conversionFunc">The function to use for the conversion of the value.</param>
        /// <returns>The created <see cref="ActionResult{TValue}"/>.</returns>
        public ActionResult<TCast> Convert<TCast>(Func<T, TCast> conversionFunc)
        {
            return conversionFunc == null
                ? throw new ArgumentNullException(nameof(conversionFunc), "No conversion function specified")
                : ConvertInternal(conversionFunc);
        }
    }
}
