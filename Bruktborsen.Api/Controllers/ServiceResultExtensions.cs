using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

public static class ServiceResultExtensions
{
    public static ActionResult ToErrorResult(this ControllerBase controller, ServiceResult result) =>
        result.Error switch
        {
            ServiceError.NotFound   => controller.NotFound(new { message = result.Message }),
            ServiceError.BadRequest => controller.BadRequest(new { message = result.Message }),
            ServiceError.Forbidden  => controller.Forbid(),
            ServiceError.Conflict   => controller.Conflict(new { message = result.Message }),
            _                       => controller.StatusCode(500)
        };
}