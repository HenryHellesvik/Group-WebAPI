using Bruktborsen.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Middleware;

// Gjør ApiException om til ProblemDetails med riktig statuskode.
// Andre unntak slippes videre og blir en vanlig 500.
public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
            return false;

        httpContext.Response.StatusCode = apiException.StatusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = apiException.StatusCode,
                Detail = apiException.Message
            }
        });
    }
}
