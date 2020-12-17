using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Hive
{
    public record HiveObjectQuery<T>(T? Value, string? Message, int StatusCode)
    {
        public ActionResult<T> Convert()
        {
            return StatusCode switch
            {
                StatusCodes.Status200OK => new OkObjectResult(Value),
                StatusCodes.Status204NoContent => new NoContentResult(),
                StatusCodes.Status400BadRequest => new BadRequestObjectResult(Message),
                StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(Message),
                StatusCodes.Status403Forbidden => new ForbidResult(),
                StatusCodes.Status404NotFound => new NotFoundObjectResult(Message),
                _ => new ObjectResult(Message) { StatusCode = StatusCode },
            };
        }
    }
}