using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using Hive.Resources;

namespace Hive
{
    /// <summary>
    /// Represents a wrapper around a result with a message and status code. Used to signify successful or failed requests.
    /// </summary>
    /// <typeparam name="T">The type of the value to wrap.</typeparam>
    public record HiveObjectQuery<T>
    {
        private enum Kind
        {
            None,
            Value,
            Message,
        }

        private static Kind KindFromStatusCode(int status) => status switch
        {
            StatusCodes.Status200OK => Kind.Value,
            StatusCodes.Status204NoContent => Kind.None,
            StatusCodes.Status400BadRequest => Kind.Message,
            StatusCodes.Status401Unauthorized => Kind.Message,
            StatusCodes.Status403Forbidden => Kind.None,
            StatusCodes.Status404NotFound => Kind.Message,
            StatusCodes.Status424FailedDependency => Kind.Value,
            _ => Kind.Message
        };

        private ActionResult<TImpl> ConvertInternal<TImpl>(Func<T, TImpl> conversionFunc)
        {
            return StatusCode switch
            {
                StatusCodes.Status200OK => new OkObjectResult(conversionFunc.Invoke(Value!)),
                StatusCodes.Status204NoContent => new NoContentResult(),
                StatusCodes.Status400BadRequest => new BadRequestObjectResult(Message),
                StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(Message),
                StatusCodes.Status403Forbidden => new EmptyStatusCodeResponse(StatusCodes.Status403Forbidden),
                StatusCodes.Status404NotFound => new NotFoundObjectResult(Message),
                StatusCodes.Status424FailedDependency => new ObjectResult(conversionFunc.Invoke(Value!)) { StatusCode = StatusCode },
                _ => new ObjectResult(Message) { StatusCode = StatusCode },
            };
        }

        /// <summary>
        /// The returned value from a query, if it exists.
        /// </summary>
        public T? Value { get; }
        /// <summary>
        /// The message to return from a query, if it exists.
        /// </summary>
        public string? Message { get; }
        /// <summary>
        /// The status code of the result from a query.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Construct an instance with a status code and a value.
        /// </summary>
        /// <param name="statusCode">Status code to provide</param>
        /// <param name="value">Value to provide</param>
        /// <exception cref="ArgumentException">Will throw if the status code is not valid</exception>
        public HiveObjectQuery(int statusCode, T value)
        {
            if (KindFromStatusCode(statusCode) != Kind.Value)
                throw new ArgumentException(Resource.StatusCode_Value_Unnecessary, nameof(statusCode));
            Value = value;
            StatusCode = statusCode;
        }
        /// <summary>
        /// Construct an instance with a status code and a message.
        /// </summary>
        /// <param name="statusCode">Status code to provide</param>
        /// <param name="message">Message to provide</param>
        /// <exception cref="ArgumentException">Will throw if the status code is not valid</exception>
        public HiveObjectQuery(int statusCode, string message)
        {
            if (KindFromStatusCode(statusCode) != Kind.Message)
                throw new ArgumentException(Resource.StatusCode_Message_Unnecessary, nameof(statusCode));
            Message = message;
            StatusCode = statusCode;
        }
        /// <summary>
        /// Construct an instance with ONLY a status code.
        /// </summary>
        /// <param name="statusCode">Status code to provide</param>
        /// <exception cref="ArgumentException">Will throw if the status code is not valid</exception>
        public HiveObjectQuery(int statusCode)
        {
            if (KindFromStatusCode(statusCode) != Kind.None)
                throw new ArgumentException(Resource.StatusCode_None_Unnecessary, nameof(statusCode));
            StatusCode = statusCode;
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
            if (conversionFunc == null)
                throw new ArgumentNullException(nameof(conversionFunc), "No conversion function specified");
            return ConvertInternal(conversionFunc);
        }
    }
}
