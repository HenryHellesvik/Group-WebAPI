namespace Bruktborsen.Api.Exceptions;

// Felles basisklasse for feil som skal bli til en bestemt HTTP-statuskode.
// GlobalExceptionHandler oversetter disse til ProblemDetails.
public abstract class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class BadRequestException(string message) : ApiException(StatusCodes.Status400BadRequest, message);

public class ForbiddenException(string message) : ApiException(StatusCodes.Status403Forbidden, message);

public class NotFoundException(string message) : ApiException(StatusCodes.Status404NotFound, message);

public class ConflictException(string message) : ApiException(StatusCodes.Status409Conflict, message);
