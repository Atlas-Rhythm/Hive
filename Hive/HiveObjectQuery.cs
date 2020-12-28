using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace Hive
{
    public record HiveObjectQuery<T>(T? Value, string? Message, int StatusCode)
    {
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

        public ActionResult<T> Convert() => ConvertInternal(conv => conv);


        public ActionResult<TCast> Convert<TCast>(Func<T, TCast> conversionFunc)
        {

            if (conversionFunc == null)
                throw new ArgumentNullException(nameof(conversionFunc), "No conversion function specified");
            return ConvertInternal(conversionFunc);
        }
    }
}